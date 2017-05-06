# ObjectValidator
Инновационность проекта ObjectValidator состоит в том, что используется быстрый алгоритм получения имени свойства. Алгоритм основан на разборе тела метода
[Parsing the IL of a Method Body](https://www.codeproject.com/Articles/14058/Parsing-the-IL-of-a-Method-Body).

## Почему не [FluentValidation](https://github.com/JeremySkinner/FluentValidation)
Typically, FluentValidation is used against a viewmodel/inputmodel **not a business entity** [[1]](https://github.com/JeremySkinner/FluentValidation/issues/260#issuecomment-220558484), [[2]](http://stackoverflow.com/a/25313887).

Для получения имени свойства в FluentValidation используется дерево выражений (expression tree). К сожалению, деревья выражений в .Net работают медленно. Деревья выражений замедляют работу со свойствами объектов в 60 раз [[3]](https://github.com/AVPolyakov/PropertyInfoBenchmark). Из-за этого рекомендуется кешировать валидаторы в статическом контексте [[4]](https://github.com/JeremySkinner/FluentValidation/wiki/b.-Creating-a-Validator#a-note-on-performance). Это усложняет передачу внешних параметров в валидатор
[[5]](https://github.com/JeremySkinner/FluentValidation/issues/449),
[[6]](http://stackoverflow.com/a/29809446),
[[1]](https://github.com/JeremySkinner/FluentValidation/issues/260#issuecomment-220558484),
[[7]](http://stackoverflow.com/q/32247571),
[[8]](http://stackoverflow.com/q/3317706).
[[9]](http://stackoverflow.com/q/18664943).
Автор FluentValidation предлагает использовать обходной путь [[1]](https://github.com/JeremySkinner/FluentValidation/issues/260#issuecomment-220558484).

>Do not forget that **your goal is to implement validation, not to use FluentValidation everywhere**. Sometimes we implement validation logic as a separate method, that works with ViewModel and fill ModelState in ASP.NET MVC.
>If you can't find solution, that match your requirements, then manual implementation would be better than _crutchful_ implementation with library [[6]](http://stackoverflow.com/a/29809446).

## Принцип работы ObjectValidator
Для задания правил валидации в `ValidationCommand` необходимо добавить функции (лямбды), которые выполняют валидацию.
 ```csharp
var message = new Message(); //объект, который проверяем
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
```
В этом коде есть ссылка на имя свойства `nameof(Message.Subject)` и получение значения свойства `message.Subject`. С помощью `IPropertyValidator` для обеих ссылок можно завести одну переменную.
 ```csharp
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
```
Теперь можно написать метод расширения `NotEmpty`:
```csharp
message.Validator().For(_ => _.Subject)
    .NotEmpty();
```
Метод `Validate` возвращает список объектов `ErrorInfo` с информацией об ошибках.
 ```csharp
var errorInfos = await command.Validate();
var errorInfo = errorInfos.Single();
Assert.Equal("Subject", errorInfo.PropertyName);
Assert.Equal("'Subject' should not be empty.", errorInfo.Message);
```
В процессе валидации если для свойства уже есть ошибка, то остальные связанные с этим свойством функции не вызываются.