# DiaryScraper
## Disclaimer
App for downloading diary.ru blogs. 

CloudFlare bypassing codes credit goes to [Corwin75](https://github.com/Corwin75): https://github.com/Corwin75/Cloudflare-Bypass

## Где скачать
Актуальные бинарники обитают [здесь](http://static.terribles.ru/scraper/)
Доступны дистрибутивы под Windows и Linux (Ubuntu, Debian, Mint). Если найдётся кто-то, желающий собрать проект под макосью, пишите в Issue. 

## Краткая инструкция
* Приложение предназначено для скачивания блогов с сайта [Diary](http://diary.ru). 
* Скачивание производится без пост-обработки в виде целых html-страниц, содержащих в себе посты и комментарии к ним. 
* Приложение имеет возможность обходить CloudFlare, которую Diary использует для защиты от DDOS

### Поля формы приложения
* Логин и пароль - оные от дневника. Никуда кроме самого Diary они не передаются.
* Адрес дневника - url дневника
* Рабочая папка - локальная директория, в которую будут складироваться данные
* Задержка между запросами - чтобы не напрягать чрезмерно сервера Diary (и не подставляться под бан по подозрению в DDOS), между обращениями к веб-страницам добавляется задержка, величину которой (в миллисекундах) определяет это поле. Эта задержка не относится к скачиванию статики. В целях безопасности программа не даст использовать задержку менее 100мс.
* Даты начала и окончания - программа ищет посты в дневнике через страницу календаря. Это позволяет ограничить скачивание дневника указанным диапазоном дат. Посты, не попадающие в заданный интервал, будут проигнорированы
* Перезаписывать - по-умолчанию если скачивание запущено повторно в ту же самую папку, страницы и изображения, которые уже были скачаны, будут проигнорированы, галка "перезаписывать" даёт программе указание повторно скачивать все страницы и изображения, даже если они уже были скачаны. 


### Рабочая папка
При скачивании в рабочей папке формируется ещё одна папка с названием, соответствующим адресу дневника. В ней создаются следующие файлы и папки:
* images - скачанные изображения (скачиваются только изображения с домена static.diary.ru, т.е. те, которые хостятся непосредстевнно на Diary)
* posts - скачанные страницы постов, каждая из которых содержит пост и все комментарии к нему
* scrape.db - база данных SQLite, содержащая соответствия URL'ов постов и изображений и наименований скачанных файлов.


### Прогресс
При работе программа показывает прогресс исполнения задания. Поиск постов производится по странице календаря дневника. В соответствии с ним определяется количество дат, в которых присутствуют посты. Прогресс считается по количеству обнаруженных и обработанных дат. 

## История версий
### 0.1.0
Первая версия, которая кое-как запускается и работает
### 0.1.1
* Починил кириллические логины и пароли
* Распараллелил закачку статики (стало чуть-чуть быстрее)
### 0.1.2
* Игнорирование ошибки 404 на статике (некоторые картинки из старых постов дайри не отдаёт)
* Добавил журналирование происходящих ошибок в рабочую папку
### 0.2.0
* Полностью переписан интерфейс, теперь у нас Angular 5 и Material design
* Ошибки, связанные с журналированием, теперь игнорируются. 
### 0.2.1
* Поиск ссылок на посты на страницах теперь регистронезависимый (исправлена ошибка для некоторых блогов)
* При неудачной попытке скачивания страницы теперь производится несколько повторных попыток
* Коды подвергнуты рефакторингу, так что есть ненулевые шансы появления новых ошибок
### 0.2.2
* Добавлено отображение ошибок в случае проблем с подключением к локальному сервису
* Добавлена индексация страниц дат для более быстрого "проматывания" уже скачанных данных
* Исправлена ошибка с таймзоной дат интервала сканирования