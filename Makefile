

all: build

restore:
	dotnet restore

build:
	dotnet build -c Release

test:
	dotnet test -c Release

pack:
	dotnet pack -c Release -o ./bin/Release

perf: Makefile
	dotnet run --project Perf/Perf.csproj -c Release

macpublish:
	rm -rf Perf/bin/Release/net6.0/osx-x64/publish
	dotnet publish Perf/Perf.csproj -r osx-x64 -c Release --self-contained true

macperf: macpublish
	Perf/bin/Release/net6.0/osx-x64/publish/Perf

mactrace: macpublish
	rm -rf *.nettrace
	rm -rf *.speedscope.json
	dotnet-trace collect -- Perf/bin/Release/net6.0/osx-x64/publish/Perf
	mv *.nettrace Perf.nettrace
	dotnet-trace convert Perf.nettrace --format Speedscope
	rm -rf *.nettrace

