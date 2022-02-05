

all: build

restore:
	dotnet restore

build:
	dotnet build -c Release

test:
	dotnet test -c Release

pack:
	dotnet pack -c Release -o ./bin/Release

spheres:
	dotnet test -v n -l "console;verbosity=detailed" --no-build --no-restore -c Release --filter "FullyQualifiedName=Tests.RayMarcherTests.SphereRepeat"
	cat Tests/bin/Release/net6.0/SphereRepeatTime.txt



