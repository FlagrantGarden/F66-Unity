using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace kuruso.Esoteric
{
  public struct Rating
  {
    public int Tier;
    public int Degree;

    public override string ToString()
    {
      return $"{this.Tier}:{this.Degree}";
    }
  }
}
