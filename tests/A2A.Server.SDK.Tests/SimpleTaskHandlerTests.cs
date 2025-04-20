using A2A.Server.SDK.Handlers;
using A2A.Server.SDK.Schema;

namespace A2A.Server.SDK.Tests
{
    /// <summary>
    /// SimpleTaskHandler的单元测试
    /// </summary>
    public class SimpleTaskHandlerTests
    {
        [Fact]
        public async System.Threading.Tasks.Task HandleTaskAsync_ShouldYieldCorrectUpdates()
        {
            // 安排
            var handler = new SimpleTaskHandler();
            var context = new TaskContext
            {
                Task = new Schema.Task
                {
                    Id = "test-task-id",
                    Status = new Schema.TaskStatus
                    {
                        State = TaskState.Submitted
                    }
                },
                UserMessage = new Message
                {
                    Role = "user",
                    Parts = new List<Part>
                    {
                        new TextPart { Text = "测试消息" }
                    }
                },
                IsCancelled = () => false
            };

            // 执行
            var updates = new List<TaskYieldUpdate>();
            await foreach (var update in handler.HandleTaskAsync(context))
            {
                updates.Add(update);
            }

            // 断言
            Assert.Equal(3, updates.Count);
            
            // 第一个更新应该是"工作中"状态
            Assert.NotNull(updates[0].StatusUpdate);
            Assert.Equal(TaskState.Working, updates[0].StatusUpdate.State);
            
            // 第二个更新应该是成品
            Assert.NotNull(updates[1].ArtifactUpdate);
            Assert.Equal("output.txt", updates[1].ArtifactUpdate.Name);
            
            // 第三个更新应该是"已完成"状态
            Assert.NotNull(updates[2].StatusUpdate);
            Assert.Equal(TaskState.Completed, updates[2].StatusUpdate.State);
        }

        [Fact]
        public async System.Threading.Tasks.Task HandleTaskAsync_WithCancellation_ShouldYieldNoCompleteUpdate()
        {
            // 安排
            var handler = new SimpleTaskHandler();
            var isCancelled = false;
            var context = new TaskContext
            {
                Task = new Schema.Task
                {
                    Id = "test-task-id",
                    Status = new Schema.TaskStatus
                    {
                        State = TaskState.Submitted
                    }
                },
                UserMessage = new Message
                {
                    Role = "user",
                    Parts = new List<Part>
                    {
                        new TextPart { Text = "测试消息" }
                    }
                },
                IsCancelled = () => isCancelled
            };

            // 执行 - 获取第一个更新后设置取消标志
            var updates = new List<TaskYieldUpdate>();
            var enumerator = handler.HandleTaskAsync(context).GetAsyncEnumerator();
            
            // 获取第一个更新
            Assert.True(await enumerator.MoveNextAsync());
            updates.Add(enumerator.Current);
            
            // 设置取消标志
            isCancelled = true;
            
            // 继续获取剩余更新
            while (await enumerator.MoveNextAsync())
            {
                updates.Add(enumerator.Current);
            }

            // 断言 - 应该只有一个状态更新
            Assert.Single(updates);
            Assert.NotNull(updates[0].StatusUpdate);
            Assert.Equal(TaskState.Working, updates[0].StatusUpdate.State);
        }

        [Fact]
        public void HandleTaskAsync_ShouldProduceWorkingStatusWithCorrectMessage()
        {
            // 安排
            var handler = new SimpleTaskHandler();
            var context = new TaskContext
            {
                Task = new Schema.Task { Id = "test-task-id" },
                UserMessage = new Message { Role = "user" },
                IsCancelled = () => false
            };

            // 执行
            var enumerator = handler.HandleTaskAsync(context).GetAsyncEnumerator();
            var moveNextResult = enumerator.MoveNextAsync().Result;
            var firstUpdate = enumerator.Current;

            // 断言
            Assert.True(moveNextResult);
            Assert.NotNull(firstUpdate.StatusUpdate);
            Assert.Equal(TaskState.Working, firstUpdate.StatusUpdate.State);
            Assert.Equal("agent", firstUpdate.StatusUpdate.Message?.Role);
            Assert.NotEmpty(firstUpdate.StatusUpdate.Message?.Parts);
            Assert.IsType<TextPart>(firstUpdate.StatusUpdate.Message?.Parts[0]);
            Assert.Equal("正在处理...", ((TextPart)firstUpdate.StatusUpdate.Message?.Parts[0]).Text);
        }

        [Fact]
        public async System.Threading.Tasks.Task HandleTaskAsync_ShouldProduceArtifactWithTaskIdInContent()
        {
            // 安排
            var taskId = "test-task-id-artifact";
            var handler = new SimpleTaskHandler();
            var context = new TaskContext
            {
                Task = new Schema.Task { Id = taskId },
                UserMessage = new Message { Role = "user" },
                IsCancelled = () => false
            };

            // 执行 - 跳过第一个状态更新，获取成品更新
            var updates = new List<TaskYieldUpdate>();
            await foreach (var update in handler.HandleTaskAsync(context))
            {
                updates.Add(update);
                if (updates.Count == 2) break; // 获取前两个更新就足够了
            }

            // 断言
            Assert.Equal(2, updates.Count);
            Assert.NotNull(updates[1].ArtifactUpdate);
            Assert.Equal("output.txt", updates[1].ArtifactUpdate.Name);
            
            // 检查成品内容包含任务ID
            Assert.NotEmpty(updates[1].ArtifactUpdate.Parts);
            var textPart = updates[1].ArtifactUpdate.Parts[0] as TextPart;
            Assert.NotNull(textPart);
            Assert.Contains(taskId, textPart.Text);
        }
        
        [Fact]
        public async System.Threading.Tasks.Task HandleTaskAsync_CompletionStatus_ShouldHaveCorrectMessage()
        {
            // 安排
            var handler = new SimpleTaskHandler();
            var context = new TaskContext
            {
                Task = new Schema.Task { Id = "test-task-id" },
                UserMessage = new Message { Role = "user" },
                IsCancelled = () => false
            };

            // 执行 - 获取所有更新
            var updates = new List<TaskYieldUpdate>();
            await foreach (var update in handler.HandleTaskAsync(context))
            {
                updates.Add(update);
            }
            var completionUpdate = updates.Last();

            // 断言
            Assert.NotNull(completionUpdate.StatusUpdate);
            Assert.Equal(TaskState.Completed, completionUpdate.StatusUpdate.State);
            Assert.Equal("agent", completionUpdate.StatusUpdate.Message?.Role);
            Assert.NotEmpty(completionUpdate.StatusUpdate.Message?.Parts);
            var textPart = completionUpdate.StatusUpdate.Message?.Parts[0] as TextPart;
            Assert.NotNull(textPart);
            Assert.Equal("完成！", textPart.Text);
        }
    }
}