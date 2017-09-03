# searchbigfiles
search string in big text files

40G的文本文件扫描，当扫描的字符串<10个时，总共花了20多分钟。
因为有的文件上G，太大了，运行会报outofmemory，所以先把所有文件保存到数据库，然后当>200M的时候，就分割文件。
一次读一个文件，在这个文件搜索字符串。当需要搜索的字符串多的时候，需要的时间更多。

后来改用了python来做，试了下搜索100多个字符串，运用多进程，总共也只花10多分钟。当然，就算只搜1个，时间也少不了多少。
详情看另一个项目里的searchhelper.py
https://github.com/sui84/pytest
