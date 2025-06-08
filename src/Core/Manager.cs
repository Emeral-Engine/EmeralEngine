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
    public class Manager<T>
    {
        public ObservableCollection<T> Information;
        private Stack<ObservableCollection<T>> Histories;
        private Stack<ObservableCollection<T>> UndoHistories;

        public Manager()
        {
            UndoHistories = new();
            Histories = new();
            Information = new ObservableCollection<T>();
            Information.CollectionChanged += (sender, e) =>
            {
                Histories.Push(new ObservableCollection<T>(Information));
                UndoHistories.Clear();
            };
        }

        public void Undo()
        {
            // Ctrl+Z
            if (0 < Histories.Count)
            {
                Information = Histories.Pop();
                UndoHistories.Push(new ObservableCollection<T>(Information));
            }
        }

        public void Redo()
        {
            // Ctrl+Y
            if (0 < UndoHistories.Count)
            {
                Information = UndoHistories.Pop();
                Histories.Push(new ObservableCollection<T>(Information));
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
