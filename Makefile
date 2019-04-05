build=./build
source=./src
exe_name=glc

all:
	mcs -out:${build}/${exe_name} ${source}/Main.cs ${source}/Engine/*

clean:
	rm -Rf ${build}/*

exe:
	${build}/${exe_name}