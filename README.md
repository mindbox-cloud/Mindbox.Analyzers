## Mindbox.Analyzers

Для локального запуска используй консольку, поменяй в ней [SolutionPath](https://github.com/mindbox-cloud/Mindbox.Analyzers/blob/8178493f939c91f536e41e2978cfa63bca04336c/Mindbox.Analyzers.ConsoleApplication/Program.cs#L12).

Если добавляешь рулы с использованием SemanticModel, помни, что консолька местами не вывозит в анализ сложных солюшенов (из нескольких проектов). Например, не сможет определить тип переменной, потому что она отдается методом из другой сборки.
В таком случае для проверки рулов лучше собрать пакет и подключить в свой солюшен:
- Через локальных фид
  - Билдишь анализатор (перед этим в csproj впиши ему любую новую версию)
  - Перейди в папку проекта анализтора в терминале и выполни dotnet pack
  - Подключи свой пакет в солюшен через локальный фид (инструкция [тут](https://github.com/mindbox-cloud/Mindbox.Framework/wiki/%D0%9A%D0%B0%D0%BA-%D0%BE%D1%82%D0%BB%D0%B0%D0%B4%D0%B8%D1%82%D1%8C-%D0%BB%D0%BE%D0%BA%D0%B0%D0%BB%D1%8C%D0%BD%D1%8B%D0%B5-%D0%B8%D0%B7%D0%BC%D0%B5%D0%BD%D0%B5%D0%BD%D0%B8%D1%8F-%D0%B2-%D0%BF%D0%B0%D0%BA%D0%B5%D1%82%D0%B5))
- Через dev версию пакета (инструкция [тут](https://github.com/mindbox-cloud/Mindbox.Framework/wiki/%D0%9A%D0%B0%D0%BA-%D0%BE%D1%82%D0%BB%D0%B0%D0%B4%D0%B8%D1%82%D1%8C-%D0%BB%D0%BE%D0%BA%D0%B0%D0%BB%D1%8C%D0%BD%D1%8B%D0%B5-%D0%B8%D0%B7%D0%BC%D0%B5%D0%BD%D0%B5%D0%BD%D0%B8%D1%8F-%D0%B2-%D0%BF%D0%B0%D0%BA%D0%B5%D1%82%D0%B5))