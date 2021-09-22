#include <mpi.h>
#include <stdio.h>
#include <semaphore.h>

struct Range
{
    int firstIndex;
    int lastIndex;
};

sem_t sem[1];
const int MAX_NUMBER = 100;
int sumArr[100];
int multArr[100];
int finalSum=0;
int finalMult=0;
int totalReturn=0;

struct Range balance_load(int cores, int numbers, int core)
{
    sem_init(&sem[0], 0, 1);
    double quotient = numbers / cores;
    double remainder = numbers % cores;
    int count = 0;
    struct Range load;

    if (core < remainder)
    {
        count = quotient + 1;
        load.firstIndex = core * count;
    }
    else
    {
        count = quotient;
        load.firstIndex = core * count + remainder;
    }
    load.lastIndex = load.firstIndex + count;

    return load;
};

int main(int argc, char **argv)
{
    int array[MAX_NUMBER];
    int processes;
    int processId;
    int arrSize;
    struct Range Range;

    MPI_Init(NULL, NULL);

    MPI_Comm_size(MPI_COMM_WORLD, &processes);
    MPI_Comm_rank(MPI_COMM_WORLD, &processId);
    if (processId == 0)
    {
        scanf("%d", &arrSize);
        if (arrSize % (processes - 1) != 0)
        {
            fprintf(stderr, "No es divisible");
            MPI_Abort(MPI_COMM_WORLD, 1);
            return 1;
        }
        for (int i = 0; i < arrSize; i++)
        {
            array[i] = i + 500;
        }
        for (int i = 0; i < processes; i++)
        {
            Range = balance_load(processes - 1, arrSize, i - 1);
            MPI_Send(&Range.firstIndex, 1, MPI_INT, i, 0, MPI_COMM_WORLD);
            MPI_Send(&Range.lastIndex, 1, MPI_INT, i, 0, MPI_COMM_WORLD);
            MPI_Send(array, arrSize, MPI_INT, i, 0, MPI_COMM_WORLD);
        }
        for (int i = 1; i < processes; i++)
        {
            int tmpSum;
            int tmpMult;
            int tmpReturn;
            MPI_Recv(&tmpSum, 1, MPI_INT, i, 0, MPI_COMM_WORLD, MPI_STATUS_IGNORE);
            MPI_Recv(&tmpMult, 1, MPI_INT, i, 0, MPI_COMM_WORLD, MPI_STATUS_IGNORE);
            MPI_Recv(&tmpReturn, 1, MPI_INT, i, 0, MPI_COMM_WORLD, MPI_STATUS_IGNORE);
            printf("Se ha recibido [%d] desde el proceso [%d]\n", tmpSum, i);
            printf("Se ha recibido [%d] desde el proceso [%d]\n", tmpMult, i);
            printf("Se ha recibido [%d] desde el proceso [%d]\n", tmpReturn, i);
            finalSum+=tmpSum;
            finalMult+=tmpMult;
            totalReturn+=tmpReturn;
            if (totalReturn == processes - 1)
            {
                printf("Suma final del arreglo es = %d \n", finalSum);
                printf("Multiplicacion final del arreglo es = %d \n", finalMult);
            }
        }
    }
    else
    {
        MPI_Recv(&Range.firstIndex, 1, MPI_INT, 0, 0, MPI_COMM_WORLD, MPI_STATUS_IGNORE);
        MPI_Recv(&Range.lastIndex, 1, MPI_INT, 0, 0, MPI_COMM_WORLD, MPI_STATUS_IGNORE);
        MPI_Recv(array, MAX_NUMBER, MPI_INT, 0, 0, MPI_COMM_WORLD, MPI_STATUS_IGNORE);
        printf("ProcessId [%d] \n", processId);
        printf("FIRST [%d], LAST [%d] \n", Range.firstIndex, Range.lastIndex);
        int value = 1;
        multArr[processId]=1;
        for (int i = Range.firstIndex; i < Range.lastIndex; i++)
        {
            if (array[i] % 2 == 0)
                sumArr[processId] += array[i];
            else
                multArr[processId] *= array[i];
        }
        printf("Sum of Process [%d] = %d\n", processId, sumArr[processId]);
        printf("Mult of Process [%d] = %d\n", processId, multArr[processId]);
        MPI_Send(&sumArr[processId], 1, MPI_INT, 0, 0, MPI_COMM_WORLD);
        MPI_Send(&multArr[processId], 1, MPI_INT, 0, 0, MPI_COMM_WORLD);
        MPI_Send(&value, 1, MPI_INT, 0, 0, MPI_COMM_WORLD);
    }

    MPI_Finalize();

    return 0;
}