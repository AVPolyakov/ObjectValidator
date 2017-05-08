using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ObjectValidator.Tests
{
    public class Tests
    {
        public Tests()
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
        }

        [Fact]
        public async Task ValidationCommand()
        {
            var message = new Message();
            var command = new ValidationCommand();
            command.Add(
                nameof(Message.Subject),
                () => string.IsNullOrWhiteSpace(message.Subject)
                    ? new ErrorInfo {
                        PropertyName = nameof(Message.Subject),
                        Message = $"'{nameof(Message.Subject)}' should not be empty."
                    }
                    : null
            );
            var errorInfos = await command.Validate();
            Assert.Equal("Subject", errorInfos.Single().PropertyName);
            Assert.Equal("'Subject' should not be empty.", errorInfos.Single().Message);
        }

        [Fact]
        public async Task PropertyValidator()
        {
            var message = new Message();
            var subject = message.Validator().For(_ => _.Subject);
            subject.Validator.Command.Add(
                subject.PropertyName(),
                () => string.IsNullOrWhiteSpace(subject.Value())
                    ? new ErrorInfo {
                        PropertyName = subject.PropertyName(),
                        Message = $"'{subject.PropertyName()}' should not be empty."
                    }
                    : null
            );
            var errorInfos = await subject.Validator.Command.Validate();
            Assert.Equal("Subject", errorInfos.Single().PropertyName);
            Assert.Equal("'Subject' should not be empty.", errorInfos.Single().Message);
        }

        [Fact]
        public async Task NotEmpty()
        {
            var message = new Message();
            var validator = message.Validator();
            validator.For(_ => _.Subject)
                .NotEmpty();
            var errorInfos = await validator.Command.Validate();
            Assert.Equal("Subject", errorInfos.Single().PropertyName);
            Assert.Equal("'Subject' should not be empty.", errorInfos.Single().Message);
        }

        [Fact]
        public async Task NestedObject()
        {
            var message = new Message {
                Person = new Person()
            };
            var validator = message.Validator();
            validator.For(_ => _.Person).Validator()
                .For(_ => _.FirstName)
                .NotEmpty();
            var errorInfos = await validator.Command.Validate();
            Assert.Equal("Person.FirstName", errorInfos.Single().PropertyName);
            Assert.Equal("'FirstName' should not be empty.", errorInfos.Single().Message);
        }

        [Fact]
        public async Task NestedCollection()
        {
            var message = new Message {
                Attachments = new List<Attachment> {
                    new Attachment(),
                    new Attachment()
                }
            };
            var validator = message.Validator();
            foreach (var attachment in validator.For(_ => _.Attachments).Validators())
            {
                attachment.For(_ => _.FileName).NotEmpty();
            }
            var errorInfos = await validator.Command.Validate();
            Assert.Equal(2, errorInfos.Count);
            Assert.Equal("Attachments[0].FileName", errorInfos[0].PropertyName);
            Assert.Equal("'FileName' should not be empty.", errorInfos[0].Message);
            Assert.Equal("Attachments[1].FileName", errorInfos[1].PropertyName);
            Assert.Equal("'FileName' should not be empty.", errorInfos[1].Message);
        }

        [Fact]
        public async Task ErrorCode()
        {
            var message = new Message();
            var validator = message.Validator();
            validator.For(_ => _.Subject)
                .NotEmpty();
            var errorInfos = await validator.Command.Validate();
            Assert.Equal("notempty_error", errorInfos.Single().Code);
        }

        [Fact]
        public async Task DisplayName()
        {
            var message = new Message();
            var validator = message.Validator();
            validator.For(_ => _.Subject, "Message subject")
                .NotEmpty();
            var errorInfos = await validator.Command.Validate();
            Assert.Equal("Subject", errorInfos.Single().PropertyName);
            Assert.Equal("Message subject", errorInfos.Single().DisplayPropertyName);
            Assert.Equal("notempty_error", errorInfos.Single().Code);
            Assert.Equal("'Message subject' should not be empty.", errorInfos.Single().Message);
        }

        [Fact]
        public async Task NotNull_Int()
        {
            var entity1 = new Entity1();
            var validator = entity1.Validator();
            validator.For(_ => _.Int1)
                .NotNull();
            var errorInfos = await validator.Command.Validate();
            Assert.Equal("Int1", errorInfos.Single().PropertyName);
            Assert.Equal("Int1", errorInfos.Single().DisplayPropertyName);
            Assert.Equal("notnull_error", errorInfos.Single().Code);
            Assert.Equal("'Int1' must not be empty.", errorInfos.Single().Message);
        }

        [Fact]
        public async Task NotNull_ObjectProperty()
        {
            var message = new Message();
            var validator = message.Validator();
            validator.For(_ => _.Person)
                .NotNull();
            var errorInfos = await validator.Command.Validate();
            Assert.Equal("Person", errorInfos.Single().PropertyName);
            Assert.Equal("Person", errorInfos.Single().DisplayPropertyName);
            Assert.Equal("notnull_error", errorInfos.Single().Code);
            Assert.Equal("'Person' must not be empty.", errorInfos.Single().Message);
        }
    }

    public class Message
    {
        public Person Person { get; set; }
        public string Subject { get; set; }
        public List<Attachment> Attachments { get; set; }
    }

    public class Person
    {
        public string FirstName { get; set; }
    }

    public class Attachment
    {
        public string FileName { get; set; }
    }

    public class Entity1
    {
        public int? Int1 { get; set; }
    }
}
