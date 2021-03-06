﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ObjectValidator.Tests
{
    public class ObjectValidatorTests
    {
        public ObjectValidatorTests()
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
                    ? new FailureData(
                        propertyName: nameof(Message.Subject),
                        errorMessage: $"'{nameof(Message.Subject)}' should not be empty.")
                    : null
            );
            var failureDatas = await command.Validate();
            Assert.Equal("Subject", failureDatas.Single().GetPropertyName());
            Assert.Equal("'Subject' should not be empty.", failureDatas.Single().ErrorMessage);
        }

        [Fact]
        public async Task PropertyValidator()
        {
            var message = new Message();
            var subject = message.Validator().For(_ => _.Subject);
            subject.Command.Add(
                subject.PropertyName,
                () => string.IsNullOrWhiteSpace(subject.Value)
                    ? new FailureData(
                        propertyName: subject.PropertyName,
                        errorMessage: $"'{subject.PropertyName}' should not be empty.")
                    : null
            );
            var failureDatas = await subject.Command.Validate();
            Assert.Equal("Subject", failureDatas.Single().GetPropertyName());
            Assert.Equal("'Subject' should not be empty.", failureDatas.Single().ErrorMessage);
        }

        [Fact]
        public async Task NotEmpty()
        {
            var message = new Message();
            var validator = message.Validator();
            validator.For(_ => _.Subject)
                .NotEmpty();
            var failureDatas = await validator.Validate();
            Assert.Equal("Subject", failureDatas.Single().GetPropertyName());
            Assert.Equal("'Subject' should not be empty.", failureDatas.Single().ErrorMessage);
        }

        [Fact]
        public async Task NotEmpty_Int()
        {
            var entity1 = new Entity1();
            var validator = entity1.Validator();
            validator.For(_ => _.Int2)
                .NotEmpty();
            var failureDatas = await validator.Validate();
            Assert.Equal("Int2", failureDatas.Single().GetPropertyName());
            Assert.Equal("'Int2' should not be empty.", failureDatas.Single().ErrorMessage);
        }

        [Fact]
        public async Task NotEmpty_NullableInt()
        {
            var entity1 = new Entity1();
            var validator = entity1.Validator();
            validator.For(_ => _.NullableInt1)
                .NotEmpty();
            var failureDatas = await validator.Validate();
            Assert.Equal("NullableInt1", failureDatas.Single().GetPropertyName());
            Assert.Equal("'NullableInt1' should not be empty.", failureDatas.Single().ErrorMessage);
        }

        [Fact]
        public async Task NotEmpty_List()
        {
            var entity1 = new Entity1();
            var validator = entity1.Validator();
            validator.For(_ => _.List1)
                .NotEmpty();
            var failureDatas = await validator.Validate();
            Assert.Equal("List1", failureDatas.Single().GetPropertyName());
            Assert.Equal("'List1' should not be empty.", failureDatas.Single().ErrorMessage);
        }

        [Fact]
        public async Task ForThis()
        {
            var entity1 = new Entity1 {List2 = new List<long> {0}};
            var validator = entity1.Validator();
            validator.For(_ => _.List2)
                .NotEmpty()
                .ForEach(item => item.For("Name1")
                    .NotEmpty());
            var failureDatas = await validator.Validate();
            Assert.Equal("List2[0]", failureDatas.Single().GetPropertyName());
            Assert.Equal("'Name1' should not be empty.", failureDatas.Single().ErrorMessage);
        }

        [Fact]
        public async Task NotEmpty_Null()
        {
            var entity1 = new Entity1 {List1 = null};
            var validator = entity1.Validator();
            validator.For(_ => _.List1)
                .NotEmpty();
            var failureDatas = await validator.Validate();
            Assert.Equal("List1", failureDatas.Single().GetPropertyName());
            Assert.Equal("'List1' should not be empty.", failureDatas.Single().ErrorMessage);
        }

        [Fact]
        public async Task NestedObject()
        {
            var message = new Message
            {
                Person = new Person()
            };
            var validator = message.Validator();
            validator.For(_ => _.Person).For(_ => _.FirstName)
                .NotEmpty();
            var failureDatas = await validator.Validate();
            Assert.Equal("Person.FirstName", failureDatas.Single().GetPropertyName());
            Assert.Equal("'FirstName' should not be empty.", failureDatas.Single().ErrorMessage);
        }

        [Fact]
        public async Task NestedCollection()
        {
            var message = new Message
            {
                Attachments = new List<Attachment>
                {
                    new(),
                    new()
                }
            };
            var validator = message.Validator();
            foreach (var attachmentValidator in validator.For(_ => _.Attachments).Validators())
            {
                attachmentValidator.For(_ => _.FileName).NotEmpty();
            }
            var failureDatas = await validator.Validate();
            Assert.Equal(2, failureDatas.Count);
            Assert.Equal("Attachments[0].FileName", failureDatas[0].GetPropertyName());
            Assert.Equal("'FileName' should not be empty.", failureDatas[0].ErrorMessage);
            Assert.Equal("Attachments[1].FileName", failureDatas[1].GetPropertyName());
            Assert.Equal("'FileName' should not be empty.", failureDatas[1].ErrorMessage);
        }

        [Fact]
        public async Task DisplayName()
        {
            var message = new Message();
            var validator = message.Validator();
            validator.For(_ => _.Subject, "Message subject")
                .NotEmpty();
            var failureDatas = await validator.Validate();
            Assert.Equal("Subject", failureDatas.Single().GetPropertyName());
            Assert.Equal("Message subject", failureDatas.Single().GetPropertyLocalizedName());
            Assert.Equal("'Message subject' should not be empty.", failureDatas.Single().ErrorMessage);
        }

        [Fact]
        public async Task NotNull_NullableInt()
        {
            var entity1 = new Entity1();
            var validator = entity1.Validator();
            validator.For(_ => _.NullableInt1)
                .NotNull();
            var failureDatas = await validator.Validate();
            Assert.Equal("NullableInt1", failureDatas.Single().GetPropertyName());
            Assert.Equal("NullableInt1", failureDatas.Single().GetPropertyLocalizedName());
            Assert.Equal("'NullableInt1' must not be empty.", failureDatas.Single().ErrorMessage);
        }

        [Fact]
        public async Task NotNull_ObjectProperty()
        {
            var message = new Message();
            var validator = message.Validator();
            validator.For(_ => _.Person)
                .NotNull();
            var failureDatas = await validator.Validate();
            Assert.Equal("Person", failureDatas.Single().GetPropertyName());
            Assert.Equal("Person", failureDatas.Single().GetPropertyLocalizedName());
            Assert.Equal("'Person' must not be empty.", failureDatas.Single().ErrorMessage);
        }

        [Fact]
        public async Task NotEqual()
        {
            var entity1 = new Entity1 {Int2 = 7};
            var validator = entity1.Validator();
            validator.For(_ => _.Int2)
                .NotEqual(7);
            var failureDatas = await validator.Validate();
            Assert.Equal("Int2", failureDatas.Single().GetPropertyName());
            Assert.Equal("Int2", failureDatas.Single().GetPropertyLocalizedName());
            Assert.Equal("'Int2' should not be equal to '7'.", failureDatas.Single().ErrorMessage);
        }

        [Fact]
        public async Task Length()
        {
            var message = new Message {Subject = "Subject1"};
            var validator = message.Validator();
            validator.For(_ => _.Subject)
                .Length(3, 5);
            var failureDatas = await validator.Validate();
            Assert.Equal("Subject", failureDatas.Single().GetPropertyName());
            Assert.Equal("Subject", failureDatas.Single().GetPropertyLocalizedName());
            Assert.Equal("'Subject' must be between 3 and 5 characters. You entered 8 characters.", failureDatas.Single().ErrorMessage);
        }

        [Fact]
        public async Task InclusiveBetween()
        {
            var entity1 = new Entity1 {Long1 = -25};
            var validator = entity1.Validator();
            validator.For(_ => _.Long1)
                .InclusiveBetween(1, 200);
            var failureDatas = await validator.Validate();
            Assert.Equal("Long1", failureDatas.Single().GetPropertyName());
            Assert.Equal("Long1", failureDatas.Single().GetPropertyLocalizedName());
            Assert.Equal("'Long1' must be between 1 and 200. You entered -25.", failureDatas.Single().ErrorMessage);
        }

        [Fact]
        public async Task ExclusiveBetween()
        {
            var entity1 = new Entity1 {Long1 = -25};
            var validator = entity1.Validator();
            validator.For(_ => _.Long1)
                .ExclusiveBetween(1, 200);
            var failureDatas = await validator.Validate();
            Assert.Equal("Long1", failureDatas.Single().GetPropertyName());
            Assert.Equal("Long1", failureDatas.Single().GetPropertyLocalizedName());
            Assert.Equal("'Long1' must be between 1 and 200. You entered -25.", failureDatas.Single().ErrorMessage);
        }

        [Fact]
        public async Task AddToPropertyValidator_WithNamedArgs()
        {
            var message = new Message {Subject = "Subject1", Body = "Body1"};
            var validator = message.Validator();
            validator.For(_ => _.Subject)
                .Add(v => v.Value == "Subject1"
                    ? v.CreateFailureData("Test message '{Subject}', '{Body}'.",
                        text => text.ReplacePlaceholderWithValue(
                            MessageFormatter.CreateTuple("Subject", v.Value),
                            MessageFormatter.CreateTuple("Body", v.Object.Body)))
                    : null);
            var failureDatas = await validator.Validate();
            Assert.Equal("Subject", failureDatas.Single().GetPropertyName());
            Assert.Equal("Subject", failureDatas.Single().GetPropertyLocalizedName());
            Assert.Equal("Test message 'Subject1', 'Body1'.", failureDatas.Single().ErrorMessage);
        }
    }

    public class Message
    {
        public Person Person { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
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
        public int? NullableInt1 { get; set; }
        public int Int2 { get; set; }
        public long Long1 { get; set; }
        public List<string> List1 { get; set; } = new();
        public List<long> List2 { get; set; }
    }
}