﻿using AdaptiveShell.LiveTiles.Models.Enums;
using AdaptiveShell.LiveTiles.Models.ObjectModel;
using AdaptiveShell.LiveTiles.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AdaptiveShell.LiveTiles.Models.BaseElements {
    [ObjectModelClass("AdaptiveText")]
    internal class AdaptiveTextField : AdaptiveChildElement, IBindingChild {
        public AdaptiveTextField(XmlTemplateParser.NotificationType context) : base(context) { }

        private const String ATTR_TEXT_ID = "id";
        private static readonly String ATTR_TEXT_LANG = "lang";
        private static readonly String ATTR_TEXT_HINT_STYLE = "hint-style";
        private static readonly String ATTR_TEXT_HINT_WRAP = "hint-wrap";
        private static readonly String ATTR_TEXT_HINT_MAX_LINES = "hint-maxLines";
        private static readonly String ATTR_TEXT_HINT_MIN_LINES = "hint-minLines";
        private static readonly String ATTR_TEXT_HINT_ALIGN = "hint-align";
        private const String ATTR_TEXT_PLACEMENT = "placement";

        public String Id { get; set; }

        private String _text = "";
        public String Text {
            get => this._text;
            set => this.SetProperty(ref this._text, value);
        }

        [ObjectModelBindingProperty("Text", "BindableString")]
        public String BindingText { get; set; }

        [ObjectModelProperty("Text", null)]
        public IObjectModelValue PropertyText {
            get {
                if (this.BindingText != null) {
                    return null;
                }

                if (this.Text == null) {
                    return null;
                }

                return new ObjectModelString(this.Text);
            }
        }

        [ObjectModelProperty("Language")]
        public String Language { get; set; }

        public TextPlacement Placement { get; set; }

        [ObjectModelProperty("HintStyle", HintStyle.Default)]
        public HintStyle HintStyle { get; set; } = HintStyle.Default;

        [ObjectModelProperty("HintWrap")]
        public Boolean? HintWrap { get; set; }

        private Int32? _hintMaxLines;
        [ObjectModelProperty("HintMaxLines")]
        public Int32? HintMaxLines {
            get => this._hintMaxLines;

            set {
                if (value != null && value.Value < 1)
                    this._hintMaxLines = 1;

                else
                    this._hintMaxLines = value;
            }
        }

        private Int32? _hintMinLines;
        [ObjectModelProperty("HintMinLines")]
        public Int32? HintMinLines {
            get => this._hintMinLines;

            set {
                if (value < 1)
                    this._hintMinLines = 1;

                else if (value > 10)
                    this._hintMinLines = 10;

                else
                    this._hintMinLines = value;
            }
        }

        [ObjectModelProperty("HintAlign", HintAlign.Default)]
        public HintAlign HintAlign { get; set; } = HintAlign.Default;

        internal void Parse(ParseResult result, XElement node, Boolean isBindingRootLevel) {
            this.Text = node.Value;
            var attributes = new AttributesHelper(node.Attributes());

            this.ParseKnownAttributes(attributes, result, isBindingRootLevel);
            this.HandleRemainingAttributes(attributes, result);
        }

        internal void ParseKnownAttributes(AttributesHelper attributes, ParseResult result, Boolean isBindingRootLevel) {
            // Max lines is supported on non-toasts, and adaptive toasts, and group/subgroups
            if (!isBindingRootLevel) {
                // hint-max-lines is optional
                Int32 hintMaxLines;
                if (TryParse(result, attributes, ATTR_TEXT_HINT_MAX_LINES, out hintMaxLines))
                    this.HintMaxLines = hintMaxLines;
            }

            // These features are supported on non-toasts, and group/subgroups
            if (!isBindingRootLevel) {
                // hint-align is optional
                HintAlign hintAlign;
                if (this.TryParseEnum(result, attributes, ATTR_TEXT_HINT_ALIGN, out hintAlign))
                    this.HintAlign = hintAlign;

                // hint-min-lines is optional
                Int32 hintMinLines;
                if (TryParse(result, attributes, ATTR_TEXT_HINT_MIN_LINES, out hintMinLines))
                    this.HintMinLines = hintMinLines;

                // hint-style is optional
                HintStyle hintStyle;
                if (this.TryParseEnum(result, attributes, ATTR_TEXT_HINT_STYLE, out hintStyle))
                    this.HintStyle = hintStyle;

                // hint-wrap is optional
                Boolean hintWrap;
                if (TryParse(result, attributes, ATTR_TEXT_HINT_WRAP, out hintWrap))
                    this.HintWrap = hintWrap;
            }

            if (this.Context == XmlTemplateParser.NotificationType.Tile) {
                this.Id = attributes.PopAttributeValue(ATTR_TEXT_ID);
            }

            TextPlacement placement;
            if (this.TryParseEnum(result, attributes, ATTR_TEXT_PLACEMENT, out placement))
                this.Placement = placement;
        }

        protected override IEnumerable<String> GetAttributesNotSupportedByVisualizer() {
            return new String[] { ATTR_TEXT_LANG };
        }

        protected override Array GetSupportedEnums<TEnum>() {
            // Override the allowed text placement values, since they depend on OS version / supported features
            if (typeof(TEnum) == typeof(TextPlacement)) {
                switch (this.Context) {
                    default:
                        return new TextPlacement[] { TextPlacement.Inline };
                }
            }

            return base.GetSupportedEnums<TEnum>();
        }

        public override ObjectModelObject ConvertToObject() {
            switch (this.Placement) {
                case TextPlacement.Attribution:
                    return new ObjectModelObject("ToastGenericAttributionText")
                    {
                        { "Text", this.Text },
                        { "Language", this.Language }
                    };

                default:
                    return base.ConvertToObject();
            }
        }
    }
}
