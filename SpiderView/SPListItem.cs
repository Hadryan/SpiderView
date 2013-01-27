﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Spider
{
    /// <summary>
    /// MenuItem for SPMenu
    /// </summary>
    [Serializable]
    public class SPListItem
    {
        public bool Touched { get; set; }
        public Color Color
        {
            get
            {
                return this.color;
            }
            set
            {
                this.color = value;
                CustomColor = true;
            }
        }
        private Color color;

        public bool CustomColor { get; set; }
        public Uri Uri { get; set; }
        public String Text { get; set; }
        public Image Icon;
        private SPListView parent;
        public SPListItem AddItem(String text, Uri uri)
        {
            SPListItem c = new SPListItem(this.Parent);
            c.Text = text;
            c.Uri = uri;
            this.Children.Add(c);
            return c;
        }
        public SPListItem AddItem(String text, Uri uri, Image icon)
        {
            SPListItem c = new SPListItem(this.Parent);
            c.Text = text;
            c.Uri = uri;
            c.Icon = icon;
            this.Children.Add(c);
            return c;
        }
        public SPListView Parent
        {
            get
            {
                return this.parent;
            }
        }
        public Boolean Selected
        {
            get;
            set;
        }
        public SPListItem GetItemByUri(Uri uri)
        {

            foreach (SPListItem item in this.Children)
            {
                if (item.Uri.ToString().Contains(uri.ToString()))
                {
                    return item;
                }
                else
                {
                    return item.GetItemByUri(uri);
                }
            }
            return null;

        }
        public bool HasUri(Uri uri, ref bool positive)
        {
            foreach (SPListItem item in this.Children)
            {
                if (item.Uri.ToString().Contains(uri.ToString()))
                {
                    positive = true;
                }
                else
                {
                    item.HasUri(uri, ref positive);
                }
            }
            return positive;

        }
        public List<SPListItem> Children { get; set; }
        public SPListItem(SPListView parent)
        {
            this.parent = parent;
            this.Children = new List<SPListItem>();
        }
        public SPListItem()
        {
        }
        public bool Expanded { get; set; }
    }
}