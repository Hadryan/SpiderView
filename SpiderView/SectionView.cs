﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Spider
{
    public partial class SectionView : UserControl
    {
        public Board Board;
        public SpiderView SpiderView;
        public SectionView()
        {
            InitializeComponent();
        }
        public SectionView(Board board, SpiderView spiderView)
        {
            
            InitializeComponent();
            this.Board = board;
            this.Controls.Add(board);
            board.AutoResize();
            board.AutoResize();
            this.AutoScroll = true;
            this.SpiderView = spiderView;
        }
        private void SectionView_Load(object sender, EventArgs e)
        {

        }
    }
}