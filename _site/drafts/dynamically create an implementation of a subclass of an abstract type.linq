<Query Kind="Program">
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Reflection.Emit</Namespace>
</Query>

void Main()
{
	Act(new ConcreteMyFoo());
//	Act(new Empty<MyFoo>());
	var empty = GetEmptyInstanceOf<MyFoo>();
	empty.Dump();
	empty.WOO();
}

// Define other methods and classes here

T GetEmptyInstanceOf<T>() where T: class
{
	var subclassType = GetSubclassOf<T>();
	
	return (T)Activator.CreateInstance(subclassType);
}

Type GetSubclassOf<T>() where T: class
{
	var builder = GetTypeBuilder(
		parent: typeof(T));
	
	builder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
	
	return builder.CreateType();
}

TypeBuilder GetTypeBuilder(
	string typeSignature = "MyDynamicType", 
	string moduleName = "MainModule", 
	Type parent = null) 
{
	var assemblyName = new AssemblyName(typeSignature);
	var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
	var moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName);
	var typeAttributes = TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout;
	return moduleBuilder.DefineType(typeSignature, typeAttributes, parent);
}



public interface IFoo{}
public abstract class MyFoo : IFoo {
	public void WOO() { "woo".Dump();}
}
class ConcreteMyFoo : MyFoo {}
//class Empty<T> : T where T: IFoo {}

void Act(MyFoo foo) {
	foo.Dump();
}