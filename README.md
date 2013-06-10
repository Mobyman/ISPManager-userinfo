ISPManager-userinfo
===================

ISPManager user getter


Программа для получения имени владельца домена с помощью ISP Manager API. Необходима конфигурация приложения:

    string[,] loginPasswords = new string[,] {
        { "server1", "login1", "password1" },
        { "server2", "login2", "password2" },
    };
    
Где server - адрес сервера, login - логин администратора, password - пароль администратора. 

Использование программы 

    > isp.exe <hostname>
    
Так же можно запустить приложение без параметров, в таком случае hostname будет извлечен из буфера обмена.
