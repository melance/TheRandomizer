using LB.Utility.Collections;

namespace TheRandomizer.Assignment;

public class LineItemDictionary : InsensitiveDictionary<LineItemList>
{
    public Boolean Contains(String key) => this.Any(li => li.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));
    public void Add(String key) => Add(key, []);
}

