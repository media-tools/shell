#include <stdio.h>
#ifdef __WIN32__
#  include <winsock2.h>
#else
#  include <sys/socket.h>
#  include <arpa/inet.h>
#  include <sys/select.h>
#  include <netinet/in.h>
#endif
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <fcntl.h>
#include <errno.h>

#include <iostream>
#include <thread>

using namespace std;
 
void die (const char *msg)
{
    perror(msg);
    exit(1);
}

#define BUFFER_SIZE 8192

void from_stdin(int sock) {
    char buff[BUFFER_SIZE];
    while (true) {
        size_t read = fread(&buff, sizeof(char), BUFFER_SIZE, stdin);
        if (read <= 0) {
            die("fread() failed.");
        }

        if (send(sock, buff, read, 0) != (int)read) {
            die("send() failed.");
        }
    }
}

void to_stdout(int sock) {
    char buff[BUFFER_SIZE];
    while (true) {
        size_t received = recv(sock, buff, sizeof(buff), 0);
        if (received <= 0) {
            die("recv() failed.");
        }
        
        if (fwrite(&buff, sizeof(char), received, stdout) != received) {
            die("fwrite() failed.");
        }

    }
}

int main (int argc, char *argv[])
{
#ifdef __WIN32__
    WORD versionWanted = MAKEWORD(1, 1);
    WSADATA wsaData;
    WSAStartup(versionWanted, &wsaData);
#endif


    int sock;
    struct sockaddr_in addr;
    char buff[256];
 
    if (argc != 4) {
        printf("Usage: %s localport remotehost remoteport\n", argv[0]);
        exit(0);
    }
 
    sock = socket(PF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (sock < 0)
        die("socket() failed");
 
    memset(&addr, 0, sizeof(addr));
    addr.sin_family = AF_INET;
    addr.sin_addr.s_addr = htonl(INADDR_ANY);
    addr.sin_port = htons(atoi(argv[1]));
#ifdef __linux__
    int yes = 1;
    if (setsockopt(sock, SOL_SOCKET, SO_REUSEADDR, &yes, sizeof(int)) == -1) {
        perror("setsockopt(SO_REUSEADDR) failed.");
        exit(1);
    }
#endif
    if (bind(sock, (const struct sockaddr *)&addr, sizeof(addr))) {
        die("bind() failed");
    }

    memset(&addr, 0, sizeof(addr));
    addr.sin_family = AF_INET;
    addr.sin_addr.s_addr = inet_addr(argv[2]);
    addr.sin_port = htons(atoi(argv[3]));
 
    while (connect(sock, (const struct sockaddr *)&addr, sizeof(addr))) {
        if (errno != ETIMEDOUT) {
            perror("connect() failed. retry in 2 sec.");
            sleep(2);
        } else {
            perror("connect() failed.");
        }
    }
 
    snprintf(buff, sizeof(buff), "Hi, I'm %d.", getpid());
    printf("sending \"%s\"\n", buff);
    if (send(sock, buff, strlen(buff) + 1, 0) != strlen(buff) + 1)
        die("send() failed.");
 
    if (recv(sock, buff, sizeof(buff), 0) <= 0)
        die("recv() failed.");
    printf("received \"%s\"\n", buff);

#ifndef __linux__
    if (_setmode(_fileno(stdin), _O_BINARY) == -1) {
        cout << "ERROR: cin to binary:" << strerror(errno);
    }
#endif

            if ( dup2(sock, 0) < 0 )
                perror("Dup stdin");
            if ( dup2(sock, 1) < 0 )
                perror("Dup stdout");


  while (true) {
   this_thread::sleep_for( chrono::milliseconds( 200 ) );
  }

    std::thread t1(from_stdin, sock);
    std::thread t2(from_stdin, sock);

    t1.join();
    t2.join();

    return 0;
}
