﻿namespace MHLab.Patch.Core.Client
{
    public interface IUpdater
    {
        void Update();
        int ProgressRangeAmount();
    }
}