using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Microsoft.Xaml.Behaviors.Media;
using Newtonsoft.Json;
using ProjectManagerPro_SOLID.Enums;
using ProjectManagerPro_SOLID.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shell;
using User = ProjectManagerPro_SOLID.Models.User;

namespace ProjectManagerPro_SOLID.Helpers
{
    public static class FirebaseHelper
    {
        private static FirebaseClient _client;
        private static FirebaseAuthProvider _authProvider;
        private static string _authToken;
        private static readonly object _lock = new();
        private static IDisposable _taskStreamSubscription;
        internal static IDisposable _commentStreamSubscription;

        public static async Task EnsureInitializedAsync()
        {
            if (_client != null) return;

            lock (_lock)
            {
                if (_client != null) return;
            }

            try
            {
                string webApiKey = Properties.Resources.webApiKey;
                string databaseUrl = Properties.Resources.BaseUrl;

                _authProvider = new FirebaseAuthProvider(new FirebaseConfig(webApiKey));

                // Anonymous auth (safe for reading public data; secure rules needed!)
                var auth = await _authProvider.SignInAnonymouslyAsync();
                _authToken = auth.FirebaseToken;

                _client = new FirebaseClient(databaseUrl);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize Firebase.", ex);
            }
        }

        internal static async Task<User> CheckUser(string username)
        {
            await EnsureInitializedAsync();
            var snapshot = await _client.Child("Users").Child(username).OnceSingleAsync<User>();
            return snapshot;
        }

        internal static async Task<List<User>> GetUsers(UserRole role)
        {
            await EnsureInitializedAsync();
            var snapshot = await _client.Child("Users").OnceSingleAsync<Dictionary<string, User>>();
            if (snapshot == null || !snapshot.Any())
                return new List<User>();
            return snapshot.Values.Where(u => u.Role == role).ToList();
        }

        internal static async Task<bool> RegisterUser(User user)
        {
            await EnsureInitializedAsync();

            try
            {
                var existingUser = await _client
                    .Child("Users")
                    .Child(user.Username)
                    .OnceSingleAsync<User>();

                if(existingUser != null)
                {
                    return false;
                }

                await _client
                    .Child("Users")
                    .Child(user.Username)
                    .PutAsync(user);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering user: {ex.Message}");
                return false;
            }
        }

        internal static async Task<bool> AddProject(Project addProject)
        {
            await EnsureInitializedAsync();

            try
            {
                await _client
                    .Child("Projects")
                    .Child(addProject.Name)
                    .PutAsync(addProject);
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error in adding project: {ex.Message}");
                return false;
            }
        }

        internal static async Task<bool> AddTask(TaskItem task)
        {
            await EnsureInitializedAsync();
            try
            {
                await _client.
                    Child("Tasks")
                    .Child(task.ProjectID)
                    .Child(task.Title)
                    .PutAsync(task);
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error in adding task: {ex.Message}");
                return false;
            }
        }

        internal static async Task<bool> AddTaskComment(Comment comment)
        {
            await EnsureInitializedAsync();
            try
            {
                await _client.
                    Child("Comments")
                    .Child(comment.TaskItemId)
                    .Child(comment.Id)
                    .PutAsync(comment);
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error in adding comment: {ex.Message}");
                return false;
            }
        }

        internal static async Task<List<Comment>> GetComments(string taskItemId)
        {
            try
            {
                if (taskItemId != null)
                {
                    var comments = await _client
                        .Child("Comments")
                        .Child(taskItemId)
                        .OnceAsync<Comment>();
                    return comments.Select(c => c.Object).ToList();
                }
                else
                {
                    var comments = await _client
                        .Child("Comments")
                        .OnceAsync<Comment>();
                    return comments.Select(c => c.Object).ToList();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading comments: {ex.Message}");
                return new List<Comment>();
            }
        }

        internal static void ListenToProjectComments(string projectId, Action<List<Comment>> onCommentsChanged)
        {
            _commentStreamSubscription?.Dispose();

            if (string.IsNullOrEmpty(projectId))
                return;

            _commentStreamSubscription = _client
                .Child($"Comments")
                .AsObservable<Comment>()
                .Subscribe(
                    async firebaseEvent =>
                    {
                        try
                        {
                            // Fetch all comments for the project
                            var comments = await GetProjectComments(projectId);
                            onCommentsChanged?.Invoke(comments);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error fetching project comments: {ex.Message}");
                        }
                    },
                    ex => Console.WriteLine($"Firebase comment stream error: {ex.Message}")
                );
        }

        internal static async Task<List<Comment>> GetProjectComments(string projectId)
        {
            try
            {
                await EnsureInitializedAsync();

                // Get all tasks for the project first
                var tasks = await GetTasks(projectId);
                var allComments = new List<Comment>();

                // Fetch comments for each task
                foreach (var task in tasks)
                {
                    var taskComments = await GetComments(task.TaskID);
                    allComments.AddRange(taskComments);
                }

                return allComments.OrderByDescending(c => c.Timestamp).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading project comments: {ex.Message}");
                return new List<Comment>();
            }
        }

        internal static void StopListeningToComments()
        {
            _commentStreamSubscription?.Dispose();
            _commentStreamSubscription = null;
        }

        internal static async Task<bool> DeleteTask(TaskItem task)
        {
            await EnsureInitializedAsync();
            try
            {
                await _client
                    .Child("Tasks")
                    .Child(task.ProjectID)
                    .Child(task.Title)
                    .DeleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in deleting task: {ex.Message}");
                return false;
            }
        }

        internal static void ListenToTaskChanges(string projectID, Action<List<TaskItem>> onTasksChanged)
        {
            _taskStreamSubscription?.Dispose();
            if (string.IsNullOrEmpty(projectID))
                return;

            _taskStreamSubscription = _client
                .Child($"Tasks/{projectID}")
                .AsObservable<TaskItem>()
                .Subscribe(
                    firebaseEvent =>
                    {
                        Task.Run(async () =>
                        {
                            var tasks = await GetTasks(projectID);
                            onTasksChanged?.Invoke(tasks);
                        });
                    },
                    ex => Console.WriteLine($"Firebase stream error: {ex.Message}")
                );
        }

        internal static void ListenToComments(string taskItemId, Action<List<Comment>> onCommentsChanged)
        {
            _commentStreamSubscription?.Dispose();
            if (string.IsNullOrEmpty(taskItemId))
            {
                _commentStreamSubscription = _client
                .Child($"Comments")
                .AsObservable<Comment>()
                .Subscribe(
                    async firebaseEvent =>
                    {
                        try
                        {
                            // Whenever a comment changes, fetch all comments again
                            var comments = await GetComments(taskItemId);
                            onCommentsChanged?.Invoke(comments);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error fetching comments: {ex.Message}");
                        }
                    },
                    ex => Console.WriteLine($"Firebase comment stream error: {ex.Message}")
                );
            }

            else
            {
                _commentStreamSubscription = _client
                    .Child($"Comments/{taskItemId}")
                    .AsObservable<Comment>()
                    .Subscribe(
                        async firebaseEvent =>
                        {
                            try
                            {
                                // Whenever a comment changes, fetch all comments again
                                var comments = await GetComments(taskItemId);
                                onCommentsChanged?.Invoke(comments);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error fetching comments: {ex.Message}");
                            }
                        },
                        ex => Console.WriteLine($"Firebase comment stream error: {ex.Message}")
                    );
            }
        }

        internal static void StopListeningToTasks()
        {
            _taskStreamSubscription?.Dispose();
            _taskStreamSubscription = null;
        }

        internal static async Task<List<TaskItem>> GetTasks(string ProjectID)
        {
            await EnsureInitializedAsync();
            var snapshot = await _client.Child($"Tasks/{ProjectID}").OnceSingleAsync<Dictionary<string, TaskItem>>();

            if (snapshot == null || !snapshot.Any())
                return new List<TaskItem>();

            return snapshot.Values.ToList();
        }

        internal static async Task<bool> UpdateTask(TaskItem task)
        {
            await EnsureInitializedAsync();

            try
            {
                if (string.IsNullOrWhiteSpace(task.ProjectID) || string.IsNullOrWhiteSpace(task.Title))
                {
                    Console.WriteLine("❌ ProjectID and Title are required to locate the task.");
                    return false;
                }

                var updates = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                JsonConvert.SerializeObject(task)
                );

                await _client
                    .Child("Tasks")
                    .Child(task.ProjectID)
                    .Child(task.Title)
                    .PatchAsync(updates);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating task: {ex.Message}");
                return false;
            }
        }

        internal static async Task<List<Project>> GetProjects()
        {
            await EnsureInitializedAsync();

            var snapshot = await _client.Child("Projects").OnceSingleAsync<Dictionary<string, Project>>();

            if (snapshot == null || !snapshot.Any())
                return new List<Project>();

            return snapshot.Values.ToList();
        }

    }
}