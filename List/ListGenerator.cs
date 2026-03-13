using LB.Utility.Random;
using TheRandomizer.Parameters;

namespace TheRandomizer.List;

internal class ListGenerator : BaseGenerator
{
    public override Boolean SupportsParameters => false;

    public List<String> Content { get; set; } = [];

    public override GeneratorResult Generate(params BaseParameter[] parameters)
    {
        var i = PseudoRNG.Instance?.NextInt32(Content.Count - 1);
        if (i != null) return new () { Text = Content[i.Value] };
        return new ();
    }

    public override List<String> VerifyDefinition()
    {
        throw new NotImplementedException();
    }
}

