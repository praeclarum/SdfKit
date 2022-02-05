

all: build

restore:
	dotnet restore

build:
	dotnet build -c Release

test:
	dotnet test -c Release

pack:
	dotnet pack -c Release -o ./bin/Release




