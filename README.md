# Чекер OnlyFans

Порядок запуска:
1. Создать в папке с программой файл unchecked.txt
2. Добавить в unchecked.txt аккаунты в формате:
email1:password1
email2:password2
3. Запустить CheckerOnlyFans.exe
4. Выбрать количество потоков
5. Ввести токен [Rucaptcha](https://rucaptcha.com?from=11507006). <i>Пример:</i> 164d510c935300r9823e13d343ch4163
6. Результаты попадут в result[XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX]

UPD:
Onlyfans имеет аж 3 капчи. Невидимая recaptcha, невидимая hcaptcha и обычная hcaptcha. Вход происходит по двум H-капчам, и они могу попастаться разным работникам(т.к. их приходится отправять раздельно из-за разных sitekey). И тогда вход не будет выполнен и результат попадёт в error.txt
