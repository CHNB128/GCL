build = ./build
sources = ./src/Repl.cs ./src/Engine/*.cs
exe_name = evil
libs = ./libs/NDesk/*.cs

all:
	mcs -out:${build}/${exe_name} ./src/Main.cs ${libs} ${sources}

clean:
	rm -Rf ${build}/*

exe:
	${build}/${exe_name}