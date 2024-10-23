/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System;

namespace AvalonDock.Controls
{
    internal class ReentrantFlag
    {
        private bool _flag = false;
        public bool CanEnter
        {
            get
            {
                return !_flag;
            }
        }

        public ReentrantFlagHandler Enter()
        {
            if (_flag)
            {
                throw new InvalidOperationException();
            }

            return new ReentrantFlagHandler(this);
        }

        public class ReentrantFlagHandler : IDisposable
        {
            private ReentrantFlag _owner;

            public ReentrantFlagHandler(ReentrantFlag owner)
            {
                _owner = owner;
                _owner._flag = true;
            }

            public void Dispose()
            {
                _owner._flag = false;
            }
        }
    }
}