FROM openjdk:8-nanoserver

COPY cassandra/v3.11.x C:/cassandra
COPY docker-cassandra.bat C:/cassandra/bin/cassandra.bat
COPY docker-cassandra.yaml C:/cassandra/conf/cassandra.yaml

EXPOSE 9160 9042

ENTRYPOINT ["C:/cassandra/bin/cassandra.bat"]
