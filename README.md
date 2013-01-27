MethodTester
============

MethodTester allows someone to execute a method in an external .NET assembly for the purpose of testing pieces of functionality.  While a dev team is building unit tests, this allows QA teams to be able to do spot checking while an application is being built.

This application is alpha for now and needs a lot of TLC, but was a POC to use in testing our application.

    MethodTester --file=C:\Path\To\DotNetAssembly.dll --type=DotNetAssembly.ClassType --method=MethodOnClass --p[0]=1 --t[0]=System.Decimal > result.txt
	
	MethodTester --file=C:\Path\To\DotNetAssembly.dll --type=DotNetAssembly.ClassType --method=MethodOnClass2 --p![0]=result.txt

There are several command line arguments:

file: This is a reference to the actual executable or assembly that contains the method you would like to execute.
type: This is a reference to the class that has your method.
method: This is a the class name you would like to execute.
method[[type1][type2][type...]]: If the method requires a generic, the generics should be explicit.
p[1..N]: These are the input parameters that are necessary for the method to execute.
t[1..N]: Most parameters can have their types automatically inferred, however, sometimes if the method is generic it requires the type to be explicit.
p![1..N]: Parameters can also be read in from an external file.

The output is the object that is being returned in the method (or value object) in XML serialized format (the result requires that the returned object be marked [Serializable]).