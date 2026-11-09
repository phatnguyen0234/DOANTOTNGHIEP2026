using UnityEngine;

namespace BlitzyUI
{
    public class EmptyScreen : Screen
    {
        public override void OnSetup()
        {
        }

        public override void OnPush(Data data)
        {
            PushFinished();
        }

        public override void OnPop()
        {
            PopFinished();
        }

        public override void OnFocus()
        {
        }

        public override void OnFocusLost()
        {
        }
    }
}

