using A2A.Server.SDK.Schema;
using A2A.Server.SDK.Utils;
using Xunit;

namespace A2A.Server.SDK.Tests
{
    /// <summary>
    /// TypeChecks工具类的单元测试
    /// </summary>
    public class TypeChecksTests
    {
        [Fact]
        public void IsObject_WithObjectValue_ReturnsTrue()
        {
            // 安排
            object obj = new { Name = "测试" };
            
            // 执行
            bool result = TypeChecks.IsObject(obj);
            
            // 断言
            Assert.True(result);
        }
        
        [Fact]
        public void IsObject_WithStringValue_ReturnsFalse()
        {
            // 安排
            object obj = "测试字符串";
            
            // 执行
            bool result = TypeChecks.IsObject(obj);
            
            // 断言
            Assert.False(result);
        }
        
        [Fact]
        public void IsObject_WithArrayValue_ReturnsFalse()
        {
            // 安排
            object obj = new[] { 1, 2, 3 };
            
            // 执行
            bool result = TypeChecks.IsObject(obj);
            
            // 断言
            Assert.False(result);
        }
        
        [Fact]
        public void IsObject_WithNullValue_ReturnsFalse()
        {
            // 安排
            object? obj = null;
            
            // 执行
            bool result = TypeChecks.IsObject(obj);
            
            // 断言
            Assert.False(result);
        }
        
        [Fact]
        public void IsTaskStatusUpdate_WithTaskStatus_ReturnsTrue()
        {
            // 安排
            var update = new A2A.Server.SDK.Schema.TaskStatus 
            { 
                State = TaskState.Working, 
                Message = new Message { Role = "agent" } 
            };
            
            // 执行
            bool result = TypeChecks.IsTaskStatusUpdate(update);
            
            // 断言
            Assert.True(result);
        }
        
        [Fact]
        public void IsTaskStatusUpdate_WithTaskYieldUpdateContainingStatus_ReturnsTrue()
        {
            // 安排
            var update = new TaskYieldUpdate
            {
                StatusUpdate = new A2A.Server.SDK.Schema.TaskStatus { State = TaskState.Working }
            };
            
            // 执行
            bool result = TypeChecks.IsTaskStatusUpdate(update);
            
            // 断言
            Assert.True(result);
        }
        
        [Fact]
        public void IsTaskStatusUpdate_WithTaskYieldUpdateNotContainingStatus_ReturnsFalse()
        {
            // 安排
            var update = new TaskYieldUpdate
            {
                ArtifactUpdate = new Artifact { Name = "test" }
            };
            
            // 执行
            bool result = TypeChecks.IsTaskStatusUpdate(update);
            
            // 断言
            Assert.False(result);
        }
        
        [Fact]
        public void IsArtifactUpdate_WithArtifact_ReturnsTrue()
        {
            // 安排
            var update = new Artifact
            {
                Name = "test.txt",
                Parts = new List<Part> { new TextPart { Text = "测试" } }
            };
            
            // 执行
            bool result = TypeChecks.IsArtifactUpdate(update);
            
            // 断言
            Assert.True(result);
        }
        
        [Fact]
        public void IsArtifactUpdate_WithTaskYieldUpdateContainingArtifact_ReturnsTrue()
        {
            // 安排
            var update = new TaskYieldUpdate
            {
                ArtifactUpdate = new Artifact { Name = "test" }
            };
            
            // 执行
            bool result = TypeChecks.IsArtifactUpdate(update);
            
            // 断言
            Assert.True(result);
        }
        
        [Fact]
        public void IsArtifactUpdate_WithTaskYieldUpdateNotContainingArtifact_ReturnsFalse()
        {
            // 安排
            var update = new TaskYieldUpdate
            {
                StatusUpdate = new A2A.Server.SDK.Schema.TaskStatus { State = TaskState.Working }
            };
            
            // 执行
            bool result = TypeChecks.IsArtifactUpdate(update);
            
            // 断言
            Assert.False(result);
        }
    }
}