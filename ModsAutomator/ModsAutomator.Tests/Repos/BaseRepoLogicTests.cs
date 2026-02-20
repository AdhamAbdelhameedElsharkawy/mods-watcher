using ModsAutomator.Data;
using ModsAutomator.Data.Interfaces;
using Moq;
using System.Data;

namespace ModsAutomator.Tests.Repos
{
    // We inherit from your BaseRepositoryTest to get the SQLite setup,
    // but the class name MUST be distinct.
    public class BaseRepoLogicTests : BaseRepositoryTest
    {
        private class TestRepo : BaseRepository
        {
            public TestRepo(IConnectionFactory f) : base(f) { }
            // Expose the protected method for testing
            public Task<T> TestExecute<T>(Func<IDbConnection, IDbTransaction?, Task<T>> action, bool reqTrans, IDbConnection? conn = null, IDbTransaction? trans = null)
                => ExecuteAsync(action, reqTrans, conn, trans);
        }

        [Fact]
        public async Task ExecuteAsync_Should_Commit_When_Successful()
        {
            // Arrange
            var mockTrans = new Mock<IDbTransaction>();
            var mockConn = new Mock<IDbConnection>();
            mockConn.Setup(c => c.BeginTransaction()).Returns(mockTrans.Object);

            var factory = new Mock<IConnectionFactory>();
            factory.Setup(f => f.CreateConnection()).Returns(mockConn.Object);

            var repo = new TestRepo(factory.Object);

            // Act
            await repo.TestExecute(async (c, t) => await Task.FromResult(true), true);

            // Assert
            mockTrans.Verify(t => t.Commit(), Times.Once);
        }
    }
}