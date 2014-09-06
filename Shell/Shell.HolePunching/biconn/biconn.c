#include <stdio.h>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <fcntl.h>
#include <errno.h>
#include <sys/select.h>
#include <netinet/in.h>
 
void die (const char *msg)
{
    perror(msg);
    exit(1);
}
 
int main (int argc, char *argv[])
{
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
    if (bind(sock, (const struct sockaddr *)&addr, sizeof(addr)))
        die("bind() failed\n");
 
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
 
    return 0;
}
