set JAVA_HOME=C:\Program Files\Java\jdk1.8.0_131
"%JAVA_HOME%\bin\javac.exe" -target 1.8 SystemTimerTester.java || exit /b 1
"%JAVA_HOME%\bin\java.exe" -classpath .\ SystemTimerTester || exit /b 1
pause