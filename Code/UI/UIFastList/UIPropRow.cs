﻿using System.Text;
using UnityEngine;
using ColossalFramework.UI;


namespace BOB
{
    /// <summary>
    /// An individual prop row.
    /// </summary>
    public class UIPropRow : UIPanel, IUIFastListRow
    {
        // Layout constants.
        public const float RowHeight = 23f;
        private const float PaddingY = 5f;
        private const float TextScale = 0.8f;
        private const float LeftMargin = 10f;
        private const float PackageMargin = 20f;
        private const float IndexWidth = 20f;
        private const float IndexLabelX = LeftMargin + PackageMargin;

        // Layout variables.
        private float labelX;

        // Panel components.
        private UIPanel panelBackground;
        private UILabel indexLabel, nameLabel;
        private UISprite lineSprite;

        // ObjectData.
		protected PrefabInfo thisPrefab;
		protected PropListItem thisItem;
		protected int index;


        // Background for each list item.
        public UIPanel Background
        {
            get
            {
                if (panelBackground == null)
                {
                    panelBackground = AddUIComponent<UIPanel>();
                    panelBackground.width = width;
                    panelBackground.height = RowHeight;
                    panelBackground.relativePosition = Vector2.zero;

                    panelBackground.zOrder = 0;
                }

                return panelBackground;
            }
        }


        /// <summary>
        /// Called when dimensions are changed, including as part of initial setup (required to set correct relative position of label).
        /// </summary>
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            if (nameLabel != null)
            {
                Background.width = width;
                nameLabel.relativePosition = new Vector2(labelX, PaddingY);
                indexLabel.relativePosition = new Vector2(IndexLabelX, PaddingY);
            }
        }


        /// <summary>
        /// Mouse click event handler - updates the selected building to what was clicked.
        /// </summary>
        /// <param name="p">Mouse event parameter</param>
        protected override void OnClick(UIMouseEventParameter p)
        {
            base.OnClick(p);
            UpdateSelection();
        }


        /// <summary>
        /// Updates current replacement selection when this item is selected.
        /// </summary>
        public virtual void UpdateSelection()
        {

        }


        /// <summary>
        /// Generates and displays a building row.
        /// </summary>
        /// <param name="data">Object to list</param>
        /// <param name="isRowOdd">If the row is an odd-numbered row (for background banding)</param>
        public void Display(object data, bool isRowOdd)
        {
            // Perform initial setup for new rows.
            if (nameLabel == null)
            {
                isVisible = true;
                canFocus = true;
                isInteractive = true;
                width = parent.width;
                height = RowHeight;

                // Add object name label.
                nameLabel = AddUIComponent<UILabel>();
                nameLabel.width = this.width - 10f;
                nameLabel.textScale = TextScale;

                // Add index text label.
                indexLabel = AddUIComponent<UILabel>();
                indexLabel.width = IndexWidth;
                indexLabel.textScale = TextScale;
                indexLabel.relativePosition = new Vector2(IndexLabelX, PaddingY);
            }

            // Add line sprite if we need to (initially hidden).
            if (lineSprite == null)
            {
                lineSprite = AddUIComponent<UISprite>();
                lineSprite.size = new Vector2(17f, 17f);
                lineSprite.relativePosition = new Vector2(3f, 3f);
                lineSprite.Hide();
            }

            // Set initial label position.
            labelX = LeftMargin;

            // See if our attached data is a raw PropInfo (e.g an available prop item as opposed to a PropListItem replacment record).
            thisPrefab = data as PrefabInfo;
            if (thisPrefab == null)
            {
                // Hide any existing line sprites; it will be re-shown as necessary.
                if (lineSprite != null)
                {
                    lineSprite.Hide();

                    // Adjust name label position to accomodate.
                    labelX += PackageMargin;
                }

                // Text to display - StringBuilder due to the amount of manipulation we're doing.
                StringBuilder displayText = new StringBuilder();

                // Not a raw PropInfo, so it should be a PropListItem replacement record.
                // Set local references.
                thisItem = data as PropListItem;
                index = thisItem.index;

                // See if this is a network prop.
                NetPropListItem thisNetItem = data as NetPropListItem;

                // Display index number if this is an individual reference.
                if (thisItem.index >= 0)
                {
                    indexLabel.text = thisItem.index.ToString();

                    // Adjust name label position to accomodate.
                    labelX += IndexWidth;
                }
                else
                {
                    indexLabel.text = "";
                }

                bool hasReplacement = false;

                // Check to see if there's a currently active individual replacement.
                if (thisItem.individualPrefab != null)
                {
                    // A replacement is currently active - include it in the text.
                    displayText.Append(PrefabLists.GetDisplayName(thisItem.individualPrefab.name));

                    // Append probability to the label, if we're showing it.
                    if (thisItem.showProbs)
                    {
                        displayText.Append(" ");
                        displayText.Append(thisItem.individualProb);
                        displayText.Append("%");
                    }

                    // Set flag.
                    hasReplacement = true;
                }
                // If no current individual replacement, check to see if there's a currently active building/network replacement.
                else if (thisItem.replacementPrefab != null)
                {
                    // A replacement is currently active - include it in the text.
                    displayText.Append(PrefabLists.GetDisplayName(thisItem.replacementPrefab.name));

                    // Append probability to the label, if we're showing it.
                    if (thisItem.showProbs)
                    {
                        displayText.Append(" ");
                        displayText.Append(thisItem.replacementProb);
                        displayText.Append("%");
                    }

                    // Set flag.
                    hasReplacement = true;

                    // Show building replacement sprite.
                    lineSprite.atlas = TextureUtils.LoadSpriteAtlas(thisNetItem == null ? "bob_single_building_small" : "bob_road_small");
                    lineSprite.spriteName = "normal";
                    lineSprite.tooltip = Translations.Translate(thisNetItem == null ? "BOB_SPR_SBL" : "BOB_SPR_SNT");
                    lineSprite.Show();
                }
                // If no current building/network replacement, check to see if any all- replacement is currently active.
                else if (thisItem.allPrefab != null)
                {
                    // An all- replacement is currently active; append name to the label.
                    displayText.Append(PrefabLists.GetDisplayName(thisItem.allPrefab.name));

                    // Append probability if this is not a network item and we're showing probs.
                    if (thisNetItem == null && thisItem.showProbs)
                    {
                        displayText.Append(" ");
                        displayText.Append(thisItem.allProb);
                        displayText.Append("%");
                    }

                    // Set flag.
                    hasReplacement = true;

                    // Show all- replacement sprite.
                    lineSprite.atlas = TextureUtils.LoadSpriteAtlas(thisNetItem == null ? "bob_buildings_small" : "bob_all_roads_small");
                    lineSprite.spriteName = "normal";
                    lineSprite.tooltip = Translations.Translate(thisNetItem == null ? "BOB_SPR_ABL" : "BOB_SPR_ANT");
                    lineSprite.Show();
                }
                // If no other replacements, chek to see if any pack replacement is currently active
                else if (thisItem.packagePrefab != null)
                {
                    // Yes; append name to the label.
                    displayText.Append(PrefabLists.GetDisplayName(thisItem.packagePrefab.name));

                    // Set flag.
                    hasReplacement = true;

                    // Show package replacement sprite.
                    lineSprite.atlas = TextureUtils.LoadSpriteAtlas("bob_prop_pack_small");
                    lineSprite.spriteName = "normal";
                    lineSprite.tooltip = Translations.Translate("BOB_SPR_PCK");
                    lineSprite.Show();
                }

                // Do we have a replacement?
                if (hasReplacement)
                {
                    // Yes; append "was" to the display name.
                    displayText.Append("; ");
                    displayText.Append(Translations.Translate("BOB_ROW_WAS"));
                    displayText.Append(" ");
                }

                // Original prefab display name.
                displayText.Append(PrefabLists.GetDisplayName(thisItem.originalPrefab.name));

                // Show original probability in brackets immediately afterwards.
                if (thisItem.showProbs)
                {
                    displayText.Append(" (");
                    displayText.Append(thisItem.originalProb);
                    displayText.Append("%)");
                }

                // Set display text.
                nameLabel.text = displayText.ToString();
            }
            else
            {
                // Attached data is a raw PropInfo; just display its (cleaned-up) name.
                nameLabel.text = PrefabLists.GetDisplayName(thisPrefab.name);
            }

            // Set label position
            nameLabel.relativePosition = new Vector2(labelX, PaddingY);

            // Set initial background as deselected state.
            Deselect(isRowOdd);
        }


        /// <summary>
        /// Highlights the selected row.
        /// </summary>
        /// <param name="isRowOdd">If the row is an odd-numbered row (for background banding)</param>
        public void Select(bool isRowOdd)
        {
            Background.backgroundSprite = "ListItemHighlight";
            Background.color = new Color32(255, 255, 255, 255);
        }


        /// <summary>
        /// Unhighlights the (un)selected row.
        /// </summary>
        /// <param name="isRowOdd">If the row is an odd-numbered row (for background banding)</param>
        public void Deselect(bool isRowOdd)
        {
            if (isRowOdd)
            {
                // Lighter background for odd rows.
                Background.backgroundSprite = "UnlockingItemBackground";
                Background.color = new Color32(0, 0, 0, 128);
            }
            else
            {
                // Darker background for even rows.
                Background.backgroundSprite = null;
            }
        }
    }
}

