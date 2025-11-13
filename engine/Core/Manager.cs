using EmeralEngine.MessageWindow;
using EmeralEngine.Project;
using EmeralEngine.Scene;
using EmeralEngine.Story;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmeralEngine.Core
{
    public class Manager<BaseInfo>
    {
        public ObservableCollection<BaseInfo> Information;
        private Stack<ObservableCollection<BaseInfo>> Histories;
        private Stack<ObservableCollection<BaseInfo>> UndoHistories;

        public Manager()
        {
            UndoHistories = new();
            Histories = new();
            Information = new ObservableCollection<BaseInfo>();
            Information.CollectionChanged += (sender, e) =>
            {
                Histories.Push(new ObservableCollection<BaseInfo>(Information));
                UndoHistories.Clear();
            };
        }

        public void Undo()
        {
            // Ctrl+Z
            if (0 < Histories.Count)
            {
                Information = Histories.Pop();
                UndoHistories.Push(new ObservableCollection<BaseInfo>(Information));
            }
        }

        public void Redo()
        {
            // Ctrl+Y
            if (0 < UndoHistories.Count)
            {
                Information = UndoHistories.Pop();
                Histories.Push(new ObservableCollection<BaseInfo>(Information));
            }
        }
    }

    public class Managers
    {
        public ProjectManager ProjectManager;
        public StoryManager StoryManager;
        public EpisodeManager EpisodeManager;
        public SceneManager SceneManager;
        public MessageWindowManager MessageWindowManager;
        public BackupManager BackupManager;
    }
}
