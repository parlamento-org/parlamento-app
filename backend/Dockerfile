FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine

WORKDIR /usr/src/app

RUN dotnet --info

# Copy csproj and restore as distinct layers
COPY global.json .
COPY Domain/Parlamento.Domain.csproj ./Domain/Parlamento.Domain.csproj
COPY Application/Parlamento.Application.csproj ./Application/Parlamento.Application.csproj
COPY Infrastructure/Parlamento.Infrastructure.csproj ./Infrastructure/Parlamento.Infrastructure.csproj
COPY src/backend.csproj ./src/backend.csproj

RUN dotnet restore src

COPY Domain ./Domain
COPY Application ./Application
COPY Infrastructure ./Infrastructure
COPY src ./src

RUN dotnet dev-certs https --trust

WORKDIR /usr/src/app/src

CMD [ "dotnet", "watch" ]
