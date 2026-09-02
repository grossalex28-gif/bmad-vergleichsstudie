using A1Testsuite.Basisklassen;

namespace A1Testsuite.Implementierungen;

public sealed class b_bmad_2_Tests : ProjektBTestsBase
{
    public b_bmad_2_Tests() : base(new BBmad2Adapter("http://localhost:5001")) { }
}
