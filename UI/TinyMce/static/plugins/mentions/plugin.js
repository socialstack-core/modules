(function (global) {
    tinymce.PluginManager.add("mentions", function (b, s) {
        var q = tinymce.html.Node,
        u = tinymce.util.JSON,
        l = !1,
        a = !1,
        d,
        p,
        k = {
            container: null,
            selected: null,
            selectedIndex: -1
        },
        c = 1,
        m = this;
        m.editor = b;
        m.url = s;
        m.handleKeyDown = function (c, b) {
            if (!this.disabledState && l) {
                if (40 == b.keyCode)
                    return this.highlightNextMentionable(), this.cancelEvent(b);
                if (38 == b.keyCode)
                    return this.highlightPreviousMentionable(), this.cancelEvent(b);
                if (13 == b.keyCode || 9 == b.keyCode)
                    return this.selectMentionable(c, this.getCurrentWord(c)), a = !1, this.cancelEvent(b);
                if (27 == b.keyCode)
                    return a = !1, this.hideMentionables(), this.cancelEvent(b)
            }
        };
        m.cancelEvent = function (a) {
            a.preventDefault();
            a.stopImmediatePropagation();
            a.stopPropagation();
            return !1
        };
        m.getLastTypedCharacter = function (a, c) {
            //var b = a.getDoc(),
			var b = m.editor.getDoc(),
            d = m.editor.selection.getSel();
            if (d.focusNode && d.focusOffset) {
                if (b = d.focusNode.nodeValue, 0 <= d.focusOffset - 1 && b && b.length >= d.focusOffset)
                    return b.charAt(d.focusOffset - 1)
            } else if (b.selection && b.selection.createRange) {
                d = b.selection.createRange();
                if (!(d.parentElement && d.moveToPoint && d.moveStart && d.getBoundingClientRect))
                    return "";
                if (null != d.parentElement())
                    try {
                        d.parentElement().focus()
                    } catch (k) {}
                d = b.selection.createRange();
                b =
                    d.getBoundingClientRect();
                0 == b.left && 0 == b.top && (d.moveToElementText(d.parentElement()), b = d.getBoundingClientRect(), b.left = b.right);
                b.left += 0 == d.boundingLeft ? b.right - b.left : 0;
                b.top = (b.bottom + b.top) / 2;
                d.moveToPoint(b.left, b.top);
                d.moveStart("character", -1);
                return d.text
            }
            return ""
        };
        m.handleKeyUp = function (c, b) {
            if (!this.disabledState) {
                var g = this.getLastTypedCharacter(c, b);
                (l || a || "@" == g) && (40 != b.keyCode && 38 != b.keyCode && 13 != b.keyCode && 27 != b.keyCode) && (g = this.getCurrentWord(c), a || (d = !0, p = ""), 256 < g.length ||
                    "" == g ? (a = !1, this.hideMentionables()) : (a = !0, 2 < g.length && (d || g.length - 1 <= p.length || g.substr(1, p.length) != p) ? (l = !0, this.showMentionables(c, g.substr(1))) : this.hideMentionables()))
            }
        };
        m.highlightNextMentionable = function () {
            k.container && (k.selected && k.container.childNodes[0].childNodes.length > k.selectedIndex + 1) && (k.selected.className = "", k.selectedIndex++, k.selected = k.container.childNodes[0].childNodes[k.selectedIndex], k.selected.className = "mceMentionablesSelected")
        };
        m.highlightPreviousMentionable = function () {
            k.container &&
            (k.selected && 0 < k.container.childNodes[0].childNodes.length && 0 < k.selectedIndex) && (k.selected.className = "", k.selectedIndex--, k.selected = k.container.childNodes[0].childNodes[k.selectedIndex], k.selected.className = "mceMentionablesSelected")
        };
        m.setHighlightdMention = function (a) {
            k.container && (k.selected && k.container.childNodes[0].childNodes.length > a && 0 <= a) && (k.selected.className = "", k.selectedIndex = a, k.selected = k.container.childNodes[0].childNodes[k.selectedIndex], k.selected.className = "mceMentionablesSelected")
        };
        m.showMentionables = function (a, c) {
            var b = this;
			
            if (!k.container) {
                k.container = document.createElement("div");
                a.dom.setAttrib(k.container, "class", "mceMentionablesList");
                a.dom.setAttrib(k.container, "style", "position:absolute; z-index: 200001;");
                var l = document.createElement("ul");
                k.container.appendChild(l);
                document.body.appendChild(k.container)
            }
            k.selected = null;
            k.container.childNodes[0].innerHTML = "";
            var m = document.createElement("li");
            m.innerHTML = tinymce.util.I18n.translate("Loading...");
            k.container.childNodes[0].appendChild(m);
            k.container.style.display = "block";
            var l = this.getWindowScrollOffset(window),
            u = a.dom.getPos(a.getContentAreaContainer()),
            q = this.getWindowScrollOffset(a.getWin()),
            s = a.dom.getPos(a.selection.getNode()),
            z = 0,
            w = 0,
            w = a.selection.getRng(),
            v = null;
			
			//console.log('showMentionables',l,u,q,w);
			
            0 == w.getClientRects().length ? w.parentElement && w.getBoundingClientRect && w.moveToPoint && w.moveStart ? (w.moveToElementText(w.parentElement()), v = w.getBoundingClientRect(), v.left = v.right, v.left += 0 == w.boundingLeft ? v.right - v.left : 0, w.moveToPoint(v.left, v.top), w.moveStart("character",
                    -1), v = w.getClientRects()[0]) : w.insertNode && (z = a.getWin().document.createElement("span"), w.insertNode(z), v = a.dom.getPos(z), v = {
                    left: v.x,
                    top: v.y,
                    width: 0,
                    height: z.offsetHeight
                }, z.parentNode.removeChild(z)) : v = w.getClientRects()[0];
            v && (!v.width && !v.height && v.right && v.bottom) && (v = {
                    width: v.right - v.left,
                    height: v.bottom - v.top,
                    left: v.left - u.x,
                    top: v.top - u.y
                });
            v ? (z = v.top + v.height, w = v.left) : (z = 1.3 * parseInt(a.dom.getStyle(a.selection.getNode(), "font-size", !0)) + s.y, w = s.x);
			
			// simplify the calc 
			k.container.style.top = u.y + l.y + z - q.y + "px";
			
            // z + u.y > window.innerHeight / 2 ? (k.container.style.bottom =
                    // document.getElementsByTagName("body")[0].outerHeight - (u.y + l.y + z - q.y) + v.height + "px", k.container.style.left = u.x + l.x + w - q.x + "px", k.container.style.top = "auto") : (k.container.style.top = u.y + l.y + z - q.y + "px", k.container.style.left = u.x + l.x + w - q.x + "px", k.container.style.bottom = "auto");
					
            k.lastSearchText = c;
            window.clearTimeout(k.searchHandle);
            var t = a.selection.getBookmark(2);
            
            k.searchHandle = window.setTimeout(function () {
                b.searchForMentions(c, function (c, h) {
                    if (k.lastSearchText == c)
                        if (k.selected = null, k.container.childNodes[0].innerHTML = "", h && 0 != h.length) {
                            d =
                                !0;
                            p = c;
                            for (var l = 0; l < h.length; l++)
                                m = document.createElement("li"), m.style.cursor = "pointer", m.innerHTML = h[l].previewhtml, m.onclick = function () {
                                    a.selection.moveToBookmark(t);
                                    b.selectMentionable(a, b.getCurrentWord(a))
                                },
                            m.onmouseover = function () {
                                b.setHighlightdMention(parseInt(a.dom.getAttrib(this, "data-index")))
                            },
                            a.dom.setAttrib(m, "data-index", l),
                            a.dom.setAttrib(m, "data-contentid", h[l].contentid),
                            a.dom.setAttrib(m, "data-contenttypeid", h[l].contenttypeid),
                            k.container.childNodes[0].appendChild(m),
                            k.selected ||
                            (k.selectedIndex = l, k.selected = m, k.selected.className = "mceMentionablesSelected")
                        } else
                            d && (p = c.substr(0, c.length - 1), d = !1), b.hideMentionables()
                })
            }, 249)
        };
        m.hideMentionables = function (a) {
            k.container && (k.container.style.display = "none", k.selected = null, k.selectedIndex = -1);
            l = !1
        };
        m.getCurrentWord = function (a) {
            var c = a.getDoc();
            a = a.selection.getSel();
            if (a.focusNode && a.focusOffset) {
                var c = a.focusNode.nodeValue,
                b = a.focusOffset - 1;
                if (null == c || 0 == c.length)
                    return "";
                a = 0;
                for (var d = b; 0 <= d; d--)
                    if ("@" == c.charAt(d)) {
                        a = d;
                        break
                    }
                c =
                    c.substr(a, b + 1 - a);
                if (0 < c.length && "@" == c.charAt(0).toString())
                    return c
            } else if (c.selection && c.selection.createRange) {
                a = c.selection.createRange();
                if (!(a.parentElement && a.moveToPoint && a.moveStart && a.getBoundingClientRect))
                    return "";
                if (null != a.parentElement())
                    try {
                        a.parentElement().focus()
                    } catch (k) {}
                a = c.selection.createRange();
                c = a.getBoundingClientRect();
                0 == c.left && 0 == c.top && (a.moveToElementText(a.parentElement()), c = a.getBoundingClientRect(), c.left = c.right);
                c.left += 0 == a.boundingLeft ? c.right - c.left : 0;
                c.top =
                    (c.bottom + c.top) / 2;
                a.moveToPoint(c.left, c.top);
                a.moveStart("word", -1);
                a.moveStart("character", -1);
                c = a.text;
                a = 0;
                for (d = c.length - 1; 0 <= d; d--)
                    if ("@" == c.charAt(d)) {
                        a = d;
                        break
                    }
                c = c.substr(a, c.length - a);
                if (0 < c.length && "@" == c.charAt(0).toString())
                    return c
            }
            return ""
        };
        m.selectMentionable = function (a, c) {
            if (!(0 >= c.length) && k.selected) {
                var b = a.selection.getRng();
                if (b.setStart)
                    b.setStart(b.startContainer, b.startOffset - c.length);
                else if (b.parentElement && b.getBoundingClientRect && b.moveToPoint && b.moveStart) {
                    var d = b.getBoundingClientRect();
                    0 == d.left && 0 == d.top && (b.moveToElementText(b.parentElement()), d = b.getBoundingClientRect(), d.left = d.right, d.left += 0 == b.boundingLeft ? d.right - d.left : 0, b.moveToPoint(d.left, d.top));
                    b.moveStart("character", -1 * c.length)
                }
                d = {
                    id: "mceMention" + (new Date).getTime(),
                    contentId: a.dom.getAttrib(k.selected, "data-contentid"),
                    contentTypeId: a.dom.getAttrib(k.selected, "data-contenttypeid")
                };
                a.selection.setRng(b);
				b = (b = k.selected.querySelector(".mentionable-suggestion-preview")) && 0 < b.length ? b.html() : k.selected.innerHTML;
                //b = (b = $(k.selected).find(".mentionable-suggestion-preview")) && 0 < b.length ? b.html() : k.selected.innerHTML;
				
                a.selection.setContent('<span contenteditable="false" class="mceItem mceMention mceNonEditable" id="' +
                    d.id + '" data-mce-json="' +  u.serialize(d).replaceAll('"', "'") + '">' + b + "</span>");
                a.nodeChanged();
                this.hideMentionables()
            }
        };
        m.isMentionSpan = function (a) {
            return a && "SPAN" === a.nodeName && this.editor.dom.hasClass(a, "mceMention")
        };
        m.spanToObject = function (a, c) {
            var b,d;
			// removed isEmpty(a) as it has changed in v5 and was no longer working
			(b = a.attr("data-mce-json").replaceAll("'",'"')) && (b = u.parse(b), b.contentId && b.contentTypeId && (d = new q("#text", 3), d.raw = !0, d.value = "[mention:" + b.contentId + ":" + b.contentTypeId + "]"));
            d ? a.replace(d) : a.remove()
        };
        m.resolveWhitespace = function (a) {
            if (!a)
                return a;
            " " == a.substr(0,
                1) && (a = "&nbsp;" + a.substr(1));
            " " == a.substr(a.length - 1) && (a = a.substr(0, a.length - 1) + "&nbsp;");
            return a
        };
        m.textToSpan = function (a) {
            var b = this,
            d = b.editor;
            if (a.parent) {
                for (var k = new q("div"), l = /\[mention:([a-fA-F0-9]{32}):([a-fA-F0-9]{32})\]/g, p, m = 0; p = l.exec(a.value); )
                    (function () {
                        var l = {
                            id: "mceMention" + c,
                            contentId: RegExp.$1,
                            contentTypeId: RegExp.$2
                        };
                        if (0 < p.index)
                            for (var r = d.parser.parse(b.resolveWhitespace(a.value.substr(m, p.index - m))).getAll("p"), t = 0; t < r.length; t++)
                                for (var s; null != (s = r[t].walk()); )
                                    k.append(s);
                        r = new q("span", 1);
                        k.append(r);
                        r.attr({
                            id: l.id,
                            "class": "mceItem mceMention mceNonEditable",
                            "data-mce-json": u.serialize(l).replaceAll('"', "'"),
                            contenteditable: "false"
                        });
                        t = new q("#text", 3);
                        t.raw = !0;
                        t.value = tinymce.util.I18n.translate("???");
                        r.append(t);
                        b.getMentionPreview(l, function (a) {
                            var c = d.dom.get(l.id);
                            c && (c.innerHTML = a)
                        })
                    })(), m = l.lastIndex, c++;
                if (0 < m) {
                    if (m < a.value.length)
                        for (var l = d.parser.parse(b.resolveWhitespace(a.value.substr(m))).getAll("p"), s = 0; s < l.length; s++)
                            for (var z; null != (z = l[s].walk()); )
                                k.append(z);
                    a.replace(k);
                    k.unwrap()
                }
            }
        };
        m.getWindowScrollOffset = function (a) {
            var c = 0,
            b = 0;
            "number" == typeof a.pageXOffset ? (c = a.pageXOffset, b = a.pageYOffset) : a.document.body && (a.document.body.scrollLeft || a.document.body.scrollTop) ? (c = a.document.body.scrollLeft, b = a.document.body.scrollTop) : a.document.documentElement && (a.document.documentElement.scrollLeft || a.document.documentElement.scrollTop) && (c = document.documentElement.scrollLeft, b = document.documentElement.scrollTop);
            return {
                x: c,
                y: b
            }
        };
        m.getMentionPreview = function (a,
            c) {
            a.contentId && a.contentTypeId ? tinymce.util.XHR.send({
                url: this.getCallbackUrl({
                    t: "p",
                    contentid: a.contentId,
                    contenttypeid: a.contentTypeId
                }),
                success: function (a) {
                    a = tinymce.util.JSON.parse(a);
                    try {
                        a && a.html ? c(a.html) : c("")
                    } catch (b) {}
                },
                error: function (a) {
                    try {
                        c("")
                    } catch (b) {}
                }
            }) : c("")
        };
        m.searchForMentions = function (a, c) {
		
			var options = {
				method: 'get',
				mode: 'cors',
				credentials: 'include'
			};
			
			var action = this.getCallbackUrl({
                    t: "s",
                    query: a
                });
				
			fetch(action, options).then(response => {
            if (response.ok) {
                return response.json();
            } 
            return null;
        }).then(data => {
            if (data) {
                try {
                    var foundMentionables = false;
                    if (data && data.categories) {
                        //v12 format
                        data.categories.map(function (category, i) {
                            if (category.label == "Members" && category.mentionables) {
                                foundMentionables = true;
                                c(a, category.mentionables);
                            }
                        });
                    }
                    else if (data && data.mentionables) {
                        //v11 format
                        foundMentionables = true;
                        c(a, data.mentionables);
                    }

                    if (!foundMentionables)
                    {
                        c(a, null);
                    }
                    // redundant v11 check 
					//data && data.mentionables ? c(a, data.mentionables) : c(a, null)
                } catch (d) {}
            }
        }).catch((reason) => {
            console.log('failed to retrieve data ' + reason);
        });

        };
        m.getCallbackUrl = function (a) {
			
            for (var c =  this.editor.getParam("mentionsLookupUrl") + "?", b = {}, d = this.editor.getParam("mentionsQuery").split(/&/g), k = 0; k < d.length; k++) {
                var l = d[k].split(/=/);
                2 == l.length && (b[l[0]] = l[1])
            }
            for (var p in a)
                b[encodeURIComponent(p)] = encodeURIComponent(a[p]);
            a = !0;
            for (p in b)
                a ? a = !1 : c += "&", c += p + "=" + b[p];
            return c
        };
        b.on("PreInit", function () {
            b.parser.addNodeFilter("#text", function (a) {
                for (var c = a.length; c--; )
                    m.textToSpan(a[c])
            });
            b.serializer.addNodeFilter("span",
                function (a, c, b) {
                c = a.length;
                for (var d; c--; )
                    d = a[c], -1 !== (d.attr("class") || "").indexOf("mceMention") && m.spanToObject(d, b)
            });
            b.on("keydown", function (a) {
                return m.handleKeyDown(b, a)
            }, !0);
            b.on("keyup", function (a) {
                return m.handleKeyUp(b, a)
            }, !0);
            b.on("click", function () {
                m.hideMentionables()
            })
        });
        b.on("ResolveName", function (a) {
            "span" === a.name && b.dom.hasClass(a.target, "mceMention") && (a.name = tinymce.util.I18n.translate("mention"))
        });
        b.ui.registry.addMenuItem("removemention", {
            text: "Remove mention",
            cmd: "mceRemoveMention",
            stateSelector: "span.mceMention",
            context: "insert",
            prependToContext: !0,
            onPostRender: function (a) {
                a.control.hide();
                b.on("NodeChange", function () {
                    var c,
                    d;
                    c = b.dom.getParent(b.selection.getStart(), function (a) {
                        return b.dom.hasClass(a, "mceMention")
                    });
                    d = b.dom.getParent(b.selection.getEnd(), function (a) {
                        return b.dom.hasClass(a, "mceMention")
                    });
                    c || d ? a.control.show() : a.control.hide()
                }, !0)
            }
        });
        b.addCommand("mceRemoveMention", function () {
            var a,
            c;
            c = b.selection.getNode();
            m.isMentionSpan(c) && (a = b.dom.getAttrib(c, "data-mce-json")) && (a = u.parse(a));
            a && b.dom.setOuterHTML(c,
                c.innerHTML)
        });
        b.ui.registry.addButton("mentionremove", {
            text: "Remove",
            onclick: function () {
                b.dom.remove(b.selection.getNode())
            }
        });
        // b.ui.registry.addContextToolbar(function (a) {
            // return b.dom.is(a, "span.mceMention") && b.getBody().contains(a)
        // }, "mentionremove")
    });
})(window);
