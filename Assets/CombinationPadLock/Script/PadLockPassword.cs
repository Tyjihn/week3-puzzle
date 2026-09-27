// Script by Marcelli Michele

using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class PadLockPassword : MonoBehaviour
{
    MoveRuller _moveRull;

    public int[] _numberPassword = {0,0,0,0};

    // Fired once when the right combination is dialled
    public UnityEvent onCorrect;

    private bool _solved = false;

    private void Awake()
    {
        _moveRull = GetComponent<MoveRuller>();
    }

    public void Password()
    {
        if (_solved) return;

        if (_moveRull._numberArray.SequenceEqual(_numberPassword))
        {
            _solved = true;
            onCorrect.Invoke();
            Debug.Log("Password correct");

            // Es. Below the for loop to disable Blinking Material after the correct password
            for (int i = 0; i < _moveRull._rullers.Count; i++)
            {
                _moveRull._rullers[i].GetComponent<PadLockEmissionColor>()._isSelect = false;
                _moveRull._rullers[i].GetComponent<PadLockEmissionColor>().BlinkingMaterial();
            }

        }
    }
}
