FROM python:3.12.1-slim-bookworm

RUN apt update 
RUN apt install net-tools iputils-ping ffmpeg nano htop procps -y
RUN pip3 install awscli

WORKDIR /scripts
COPY Run.sh Run.sh
RUN chmod +x Run.sh
COPY record.sh record.sh
RUN chmod +x record.sh
COPY stores3.sh stores3.sh
RUN chmod +x stores3.sh
COPY rename.sh rename.sh
RUN chmod +x rename.sh

WORKDIR /recordings
ENTRYPOINT [ "/scripts/Run.sh" ] 