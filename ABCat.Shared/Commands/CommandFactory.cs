using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using JetBrains.Annotations;

namespace ABCat.Shared.Commands
{
    public class CommandFactory
    {
        private readonly Dictionary<string, ICommand> _commands = new Dictionary<string, ICommand>();

        public ICommand Get([NotNull] Action action, [NotNull] string commandName)
        {
            if (_commands.TryGetValue(commandName, out var result))
            {
                return result;
            }

            result = new DelegateCommand(action);
            _commands.Add(commandName, result);
            return result;
        }

        public ICommand Get([NotNull] Action action, [NotNull] Func<bool> canExecute, [CallerMemberName] string commandName = null)
        {
            if (_commands.TryGetValue(commandName, out var result))
            {
                return result;
            }

            result = new DelegateCommand(action, canExecute);
            _commands.Add(commandName, result);
            return result;
        }

        public ICommand Get<T>([NotNull] Action<T> action, [NotNull] string commandName)
        {
            if (_commands.TryGetValue(commandName, out var result))
            {
                return result;
            }

            result = new DelegateCommand<T>(action);
            _commands.Add(commandName, result);
            return result;
        }

        public ICommand Get<T>([NotNull] Action<T> action, [NotNull] Func<T, bool> canExecute,
            [NotNull] string commandName)
        {
            if (_commands.TryGetValue(commandName, out var result))
            {
                return result;
            }

            result = new DelegateCommand<T>(action, canExecute);
            _commands.Add(commandName, result);
            return result;
        }

        public void UpdateAll()
        {
            foreach (var command in _commands.Values.OfType<DelegateCommandBase>())
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }
}