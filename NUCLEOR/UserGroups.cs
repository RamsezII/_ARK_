﻿using _UTIL_;
using UnityEngine;

namespace _ARK_
{
    public interface IMouseUser : IUserGroup
    {
    }

    public interface IKeyboardUser : IUserGroup
    {
    }

    public interface IInputsUser : IUserGroup
    {
    }

    public sealed class GroupUser : IMouseUser, IKeyboardUser, IInputsUser
    {
    }

    partial class NUCLEOR : IMouseUser, IInputsUser
    {
        public static readonly UserGroup<IMouseUser> mouseUsers = new();
        public static readonly UserGroup<IKeyboardUser> keyboardUsers = new();
        public static readonly UserGroup<IInputsUser> inputsUsers = new();

        float last_ALT;

        //----------------------------------------------------------------------------------------------------------

        void AwakeUserGroups()
        {
            ClearUserGroups();
            mouseUsers.isUsed.AddListener(value =>
            {
                Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = value && Application.isEditor;
            });
        }

        //----------------------------------------------------------------------------------------------------------

        void UpdateAltPress()
        {
            if (Input.GetKeyDown(KeyCode.LeftAlt))
            {
                float time = Time.unscaledTime;
                if (time - last_ALT < 0.3f)
                    mouseUsers.ToggleUser(this);
                last_ALT = time;
            }
        }

        //----------------------------------------------------------------------------------------------------------

        public static void ClearUserGroups()
        {
            mouseUsers.Clear();
            keyboardUsers.Clear();
            inputsUsers.Clear();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}