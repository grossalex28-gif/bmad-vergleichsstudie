using A1Testsuite.Basisklassen;

namespace A1Testsuite.Implementierungen;

public sealed class a_bmad_2_Tests : ProjektATestsBase
{
    public a_bmad_2_Tests() : base(new ABmad2Adapter("http://localhost:5001")) { }
}
