using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ObjectValidator.Tests
{
    public class Tests
    {
        [Fact]
        public async Task ValidationCommand()
        {
            var message = new Message();
            var command = new ValidationCommand();
            command.Add(
                nameof(Message.Subject),
                () => string.IsNullOrEmpty(message.Subject)
                    ? new ErrorInfo {
                        PropertyName = nameof(Message.Subject),
                        Message = $"'{nameof(Message.Subject)}' should not be empty."
                    }
                    : null
            );
            var errorInfos = await command.Validate();
            var errorInfo = errorInfos.Single();
            Assert.Equal("Subject", errorInfo.PropertyName);
            Assert.Equal("'Subject' should not be empty.", errorInfo.Message);
        }

        [Fact]
        public async Task PropertyValidator()
        {
            var message = new Message();
            var subject = message.Validator().For(_ => _.Subject);
            subject.Validator.Command.Add(
                subject.PropertyName(),
                () => string.IsNullOrEmpty(subject.Value())
                    ? new ErrorInfo {
                        PropertyName = subject.PropertyName(),
                        Message = $"'{subject.PropertyName()}' should not be empty."
                    }
                    : null
            );
            var errorInfos = await subject.Validator.Command.Validate();
            var errorInfo = errorInfos.Single();
            Assert.Equal("Subject", errorInfo.PropertyName);
            Assert.Equal("'Subject' should not be empty.", errorInfo.Message);
        }

        [Fact]
        public async Task NotEmpty()
        {
            var message = new Message();
            var validator = message.Validator();
            validator.For(_ => _.Subject)
                .NotEmpty();
            var errorInfos = await validator.Command.Validate();
            var errorInfo = errorInfos.Single();
            Assert.Equal("Subject", errorInfo.PropertyName);
            Assert.Equal("'Subject' should not be empty.", errorInfo.Message);
        }
    }

    public class Message
    {
        public string Subject { get; set; }
    }
}
