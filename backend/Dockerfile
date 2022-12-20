FROM mcr.microsoft.com/dotnet/sdk:6.0.402-alpine3.16-amd64

WORKDIR /usr/src/app

RUN dotnet --info

# Copy csproj and restore as distinct layers
COPY src/backend.csproj .

RUN dotnet restore

COPY src .

RUN dotnet dev-certs https --trust

WORKDIR /usr/src/app/src

CMD [ "dotnet", "watch" ]
