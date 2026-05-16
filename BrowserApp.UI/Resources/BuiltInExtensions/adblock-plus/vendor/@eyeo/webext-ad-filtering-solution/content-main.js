/******/ (() => { // webpackBootstrap
/******/ 	"use strict";

;// ../../node_modules/uuid/dist/esm-browser/native.js
const randomUUID = typeof crypto !== 'undefined' && crypto.randomUUID && crypto.randomUUID.bind(crypto);
/* harmony default export */ const esm_browser_native = ({
  randomUUID
});
;// ../../node_modules/uuid/dist/esm-browser/rng.js
// Unique ID creation requires a high quality random # generator. In the browser we therefore
// require the crypto API and do not support built-in fallback to lower quality random number
// generators (like Math.random()).
let getRandomValues;
const rnds8 = new Uint8Array(16);
function rng() {
  // lazy load so that environments that need to polyfill have a chance to do so
  if (!getRandomValues) {
    // getRandomValues needs to be invoked in a context where "this" is a Crypto implementation.
    getRandomValues = typeof crypto !== 'undefined' && crypto.getRandomValues && crypto.getRandomValues.bind(crypto);

    if (!getRandomValues) {
      throw new Error('crypto.getRandomValues() not supported. See https://github.com/uuidjs/uuid#getrandomvalues-not-supported');
    }
  }

  return getRandomValues(rnds8);
}
;// ../../node_modules/uuid/dist/esm-browser/stringify.js

/**
 * Convert array of 16 byte values to UUID string format of the form:
 * XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
 */

const byteToHex = [];

for (let i = 0; i < 256; ++i) {
  byteToHex.push((i + 0x100).toString(16).slice(1));
}

function unsafeStringify(arr, offset = 0) {
  // Note: Be careful editing this code!  It's been tuned for performance
  // and works in ways you may not expect. See https://github.com/uuidjs/uuid/pull/434
  return byteToHex[arr[offset + 0]] + byteToHex[arr[offset + 1]] + byteToHex[arr[offset + 2]] + byteToHex[arr[offset + 3]] + '-' + byteToHex[arr[offset + 4]] + byteToHex[arr[offset + 5]] + '-' + byteToHex[arr[offset + 6]] + byteToHex[arr[offset + 7]] + '-' + byteToHex[arr[offset + 8]] + byteToHex[arr[offset + 9]] + '-' + byteToHex[arr[offset + 10]] + byteToHex[arr[offset + 11]] + byteToHex[arr[offset + 12]] + byteToHex[arr[offset + 13]] + byteToHex[arr[offset + 14]] + byteToHex[arr[offset + 15]];
}

function stringify(arr, offset = 0) {
  const uuid = unsafeStringify(arr, offset); // Consistency check for valid UUID.  If this throws, it's likely due to one
  // of the following:
  // - One or more input array values don't map to a hex octet (leading to
  // "undefined" in the uuid)
  // - Invalid input values for the RFC `version` or `variant` fields

  if (!validate(uuid)) {
    throw TypeError('Stringified UUID is invalid');
  }

  return uuid;
}

/* harmony default export */ const esm_browser_stringify = ((/* unused pure expression or super */ null && (stringify)));
;// ../../node_modules/uuid/dist/esm-browser/v4.js




function v4(options, buf, offset) {
  if (esm_browser_native.randomUUID && !buf && !options) {
    return esm_browser_native.randomUUID();
  }

  options = options || {};
  const rnds = options.random || (options.rng || rng)(); // Per 4.4, set bits for version and `clock_seq_hi_and_reserved`

  rnds[6] = rnds[6] & 0x0f | 0x40;
  rnds[8] = rnds[8] & 0x3f | 0x80; // Copy bytes to buffer, if provided

  if (buf) {
    offset = offset || 0;

    for (let i = 0; i < 16; ++i) {
      buf[offset + i] = rnds[i];
    }

    return buf;
  }

  return unsafeStringify(rnds);
}

/* harmony default export */ const esm_browser_v4 = (v4);
;// ../../node_modules/@eyeo/snippets/webext/main.mjs
/*!
 * This file is part of eyeo's Anti-Circumvention Snippets module (@eyeo/snippets),
 * Copyright (C) 2006-present eyeo GmbH
 * 
 * @eyeo/snippets is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
 * 
 * @eyeo/snippets is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with @eyeo/snippets.  If not, see <http://www.gnu.org/licenses/>.
 */

let currentEnvironment = {initial: true};
const callback = (environment, ...filters) => {
const e=Proxy,{apply:t,bind:n,call:r}=Function,o=r.bind(t),s=r.bind(n),i=r.bind(r),a={get:(e,t)=>s(r,e[t])},c=t=>new e(t,a),l=(t,n)=>new e(t,{apply:(e,t,r)=>o(n,t,r)}),p={get:(e,t)=>s(e[t],e)},u=t=>new e(t,p),{assign:f,defineProperties:d,freeze:h,getOwnPropertyDescriptor:g,getOwnPropertyDescriptors:y,getPrototypeOf:m}=u(Object),{hasOwnProperty:w}=c({}),{species:v}=Symbol,b={get(e,t){const n=e[t];class r extends n{}const o=y(n.prototype);delete o.constructor,h(d(r.prototype,o));const s=y(n);return delete s.length,delete s.prototype,s[v]={value:r},h(d(r,s))}},E=t=>new e(t,b);"undefined"!=typeof currentEnvironment&&currentEnvironment.initial&&"undefined"!=typeof environment&&(currentEnvironment=environment);const $=()=>"undefined"!=typeof currentEnvironment?currentEnvironment:"undefined"!=typeof environment?environment:{};"undefined"==typeof globalThis&&(window.globalThis=window);const{apply:S,ownKeys:T}=u(Reflect),x=$(),R="world"in x,k=R&&"ISOLATED"===x.world,A=R&&"MAIN"===x.world,j="object"==typeof chrome&&!!chrome.runtime,L="object"==typeof browser&&!!browser.runtime,M=!A&&(k||j||L),P=e=>M?e:O(e,F(e)),{create:O,defineProperties:N,defineProperty:C,freeze:I,getOwnPropertyDescriptor:W,getOwnPropertyDescriptors:F}=u(Object),D=u(globalThis),H=M?globalThis:E(globalThis),{Map:z,RegExp:B,Set:U,WeakMap:q,WeakSet:V}=H,G=(e,t,n=null)=>{const r=T(t);for(const o of T(e)){if(r.includes(o))continue;const s=W(e,o);if(n&&"value"in s){const{value:e}=s;"function"==typeof e&&(s.value=n(e))}C(t,o,s)}},J=e=>{const t=H[e];class n extends t{}const{toString:r,valueOf:o}=t.prototype;N(n.prototype,{toString:{value:r},valueOf:{value:o}});const s=e.toLowerCase(),i=e=>function(){const t=S(e,this,arguments);return typeof t===s?new n(t):t};return G(t,n,i),G(t.prototype,n.prototype,i),n},X=I({frozen:new q,hidden:new V,iframePropertiesToAbort:{read:new U,write:new U},abortedIframes:new q}),_=new B("^[A-Z]"),K=M&&(j&&chrome||L&&browser)||void 0;var Q=new Proxy(new z([["chrome",K],["browser",K],["isExtensionContext",M],["variables",X],["console",P(console)],["document",globalThis.document],["JSON",P(JSON)],["Map",z],["Math",P(Math)],["Number",M?Number:J("Number")],["RegExp",B],["Set",U],["String",M?String:J("String")],["WeakMap",q],["WeakSet",V],["MouseEvent",MouseEvent]]),{get(e,t){if(e.has(t))return e.get(t);let n=globalThis[t];return"function"==typeof n&&(n=(_.test(t)?H:D)[t]),e.set(t,n),n},has:(e,t)=>e.has(t)});const Y={WeakSet:WeakSet,WeakMap:WeakMap,WeakValue:class{has(){return!1}set(){}}},{apply:Z}=Reflect;const{Map:ee,WeakMap:te,WeakSet:ne,setTimeout:re}=Q;let oe=!0,se=e=>{e.clear(),oe=!oe};var ie=function(e){const{WeakSet:t,WeakMap:n,WeakValue:r}=this||Y,o=new t,s=new n,i=new r;return function(t){if(o.has(t))return t;if(s.has(t))return s.get(t);if(i.has(t))return i.get(t);const n=Z(e,this,arguments);return o.add(n),n!==t&&("object"==typeof t&&t?s:i).set(t,n),n}}.bind({WeakMap:te,WeakSet:ne,WeakValue:class extends ee{set(e,t){return oe&&(oe=!oe,re(se,0,this)),super.set(e,t)}}});const{concat:ae,includes:ce,join:le,reduce:pe,unshift:ue}=c([]),{Map:fe,WeakMap:de}=E(globalThis),he=new fe,ge=e=>{const t=(e=>{const t=[];let n=e;for(;n;){if(he.has(n))ue(t,he.get(n));else{const e=y(n);he.set(n,e),ue(t,e)}n=m(n)}return ue(t,{}),o(f,null,t)})("function"==typeof e?e.prototype:e),n={get(e,n){if(n in t){const{value:r,get:o}=t[n];if(o)return i(o,e);if("function"==typeof r)return s(r,e)}return e[n]},set(e,n,r){if(n in t){const{set:o}=t[n];if(o)return i(o,e,r),!0}return e[n]=r,!0}};return e=>new Proxy(e,n)},{isExtensionContext:ye,Array:me,Number:we,String:ve,Object:be}=Q,{isArray:Ee}=me,{getOwnPropertyDescriptor:$e,setPrototypeOf:Se}=be,{toString:Te}=be.prototype,{slice:xe}=ve.prototype,{get:Re}=$e(Node.prototype,"nodeType"),ke=ye?{}:{Attr:ge(Attr),CanvasRenderingContext2D:ge(CanvasRenderingContext2D),CSSStyleDeclaration:ge(CSSStyleDeclaration),Document:ge(Document),Element:ge(Element),HTMLCanvasElement:ge(HTMLCanvasElement),HTMLElement:ge(HTMLElement),HTMLImageElement:ge(HTMLImageElement),HTMLScriptElement:ge(HTMLScriptElement),MutationRecord:ge(MutationRecord),Node:ge(Node),ShadowRoot:ge(ShadowRoot),get CSS2Properties(){return ke.CSSStyleDeclaration}},Ae=(e,t)=>{if("Element"!==t&&t in ke)return ke[t](e);if(Ee(e))return Se(e,me.prototype);const n=(e=>i(xe,i(Te,e),8,-1))(e);if(n in ke)return ke[n](e);if(n in Q)return Se(e,Q[n].prototype);if("nodeType"in e)switch(i(Re,e)){case 1:if(!(t in ke))throw new Error("unknown hint "+t);return ke[t](e);case 2:return ke.Attr(e);case 3:return ke.Node(e);case 9:return ke.Document(e)}throw new Error("unknown brand "+n)};var je=ye?e=>e===window||e===globalThis?Q:e:ie(((e,t="Element")=>{if(e===window||e===globalThis)return Q;switch(typeof e){case"object":return e&&Ae(e,t);case"string":return new ve(e);case"number":return new we(e);default:throw new Error("unsupported value")}}));const Le={get(e,t){const n=e;for(;!w(e,t);)e=m(e);const{get:r,set:s}=g(e,t);return function(){return arguments.length?o(s,n,arguments):i(r,n)}}},Me=t=>new e(t,Le);let{Math:Pe,setInterval:Oe,performance:Ne}=je(window);const Ce={mark(){},end(){},toString:()=>"{mark(){},end(){}}"};let Ie=!0;function We(e,t=10){if(Ie)return Ce;function n(){let e=je([]);for(let{name:t,duration:n}of Ne.getEntriesByType("measure"))e.push({name:t,duration:n});e.length&&Ne.clearMeasures()}return We[e]||(We[e]=Oe(n,Pe.round(6e4/Pe.min(60,t)))),{mark(){Ne.mark(e)},end(t=!1){Ne.measure(e,e);const r=Ne.getEntriesByName(e,"measure"),o=r.length>0?r[r.length-1]:null;console.log("PROFILER:",o),Ne.clearMarks(e),t&&(clearInterval(We[e]),delete We[e],n())}}}let Fe=!1;function De(){return Fe}const{console:He}=je(window),ze=()=>{};function Be(...e){let{mark:t,end:n}=We("log");if(De()){const t=["%c DEBUG","font-weight: bold;"],n=e.indexOf("error"),r=e.indexOf("warn"),o=e.indexOf("success"),s=e.indexOf("info");-1!==n?(t[0]+=" - ERROR",t[1]+="color: red; border:2px solid red",je(e).splice(n,1)):-1!==r?(t[0]+=" - WARNING",t[1]+="color: orange; border:2px solid orange ",je(e).splice(r,1)):-1!==o?(t[0]+=" - SUCCESS",t[1]+="color: green; border:2px solid green",je(e).splice(o,1)):-1!==s&&(t[1]+="color: black;",je(e).splice(s,1)),je(e).unshift(...t)}t(),He.log(...e),n()}function Ue(e){return s(De()?Be:ze,null,e)}let{Array:qe,Math:Ve,RegExp:Ge}=je(window);function Je(e){let{length:t}=e;if(t>1&&"/"===e[0]){let n="/"===e[t-1];if(n||t>2&&je(e).endsWith("/i")){let t=[je(e).slice(1,n?-1:-2)];return n||t.push("i"),new Ge(...t)}}return new Ge(je(e).replace(/[-/\\^$*+?.()|[\]{}]/g,"\\$&"))}function Xe(){return je(Ve.floor(2116316160*Ve.random()+60466176)).toString(36)}function _e(e){return je(qe.from(e)).map((e=>`'${e}'`)).join(" ")}let{parseFloat:Ke,variables:Qe,clearTimeout:Ye,fetch:Ze,setTimeout:et,Array:tt,Error:nt,Map:rt,Object:ot,ReferenceError:st,Set:it,WeakMap:at}=je(window),{onerror:ct}=Me(window),lt=Node.prototype,pt=Element.prototype,ut=null;function ft(e,t,n,r=!0){let o=je(t),s=o.indexOf(".");if(-1==s){let o=ot.getOwnPropertyDescriptor(e,t);if(o&&!o.configurable)return;let s=ot.assign({},n,{configurable:r});if(!o&&!s.get&&s.set){let n=e[t];s.get=()=>n}return void ot.defineProperty(e,t,s)}let i=o.slice(0,s).toString();t=o.slice(s+1).toString();let a=e[i];!a||"object"!=typeof a&&"function"!=typeof a||ft(a,t,n);let c=ot.getOwnPropertyDescriptor(e,i);if(c&&!c.configurable)return;ut||(ut=new at),ut.has(e)||ut.set(e,new rt);let l=ut.get(e);if(l.has(i))return void l.get(i).set(t,n);let p=new rt([[t,n]]);l.set(i,p),ot.defineProperty(e,i,{get:()=>a,set(e){if(a=e,a&&("object"==typeof a||"function"==typeof a))for(let[e,t]of p)ft(a,e,t)},configurable:r})}function dt(e){let t=ct();ct(((...n)=>{let r=n.length&&n[0];return!("string"!=typeof r||!je(r).includes(e))||("function"==typeof t?o(t,this,n):void 0)}))}function ht(e,t,n,r="",o=!0){let s=Ue(e);if(!n)return void s("error","no property to abort on read");let i=Xe();s("info",`aborting on ${n} access`),ft(t,n,{get:function(){throw s("success",`${n} access aborted`,`\nFILTER: ${e} ${r}`),new st(i)},set(){}},o),dt(i)}function gt(e,t,n,r="",o=!0){let s=Ue(e);if(!n)return void s("error","no property to abort on write");let i=Xe();s("info",`aborting when setting ${n}`),ft(t,n,{set:function(){throw s("success",`setting ${n} aborted`,`\nFILTER: ${e} ${r}`),new st(i)}},o),dt(i)}function yt(e,t=!1,n=!1){let r=Qe.abortedIframes,s=Qe.iframePropertiesToAbort;const a=_e(e);for(let o of tt.from(window.frames))if(r.has(o))for(let s of e)t&&r.get(o).read.add({property:s,formattedProperties:a}),n&&r.get(o).write.add({property:s,formattedProperties:a});for(let r of e)t&&s.read.add({property:r,formattedProperties:a}),n&&s.write.add({property:r,formattedProperties:a});function c(){for(let e of tt.from(window.frames)){r.has(e)||r.set(e,{read:new it(s.read),write:new it(s.write)});let t=r.get(e).read;if(t.size>0){let n=tt.from(t);t.clear();for(let{property:t,formattedProperties:r}of n)ht("abort-on-iframe-property-read",e,t,r)}let n=r.get(e).write;if(n.size>0){let t=tt.from(n);n.clear();for(let{property:n,formattedProperties:r}of t)gt("abort-on-iframe-property-write",e,n,r)}}}c(),r.has(document)||(r.set(document,!0),function(e){let t;function n(e,t){for(let n of t){ft(e,n,r(e,n))}}function r(t,n){let r=t[n];return{get:()=>function(...t){let n;return n=o(r,this,t),e&&e(),n}}}function s(t,n){let r=ot.getOwnPropertyDescriptor(t,n),{set:o}=r||{};return{set(t){let n;return n=i(o,this,t),e&&e(),n}}}n(lt,["appendChild","insertBefore","replaceChild"]),n(pt,["append","prepend","replaceWith","after","before","insertAdjacentElement","insertAdjacentHTML"]),t=s(pt,"innerHTML"),ft(pt,"innerHTML",t),t=s(pt,"outerHTML"),ft(pt,"outerHTML",t)}(c))}let{Object:mt}=window;function wt(e,t){if(!(e instanceof mt))return;let n=e,r=je(t).split(".");if(0===r.length)return;for(let e=0;e<r.length-1;e++){let t=r[e];if(!w(n,t))return;if(n=n[t],!(n instanceof mt))return}let o=r[r.length-1];return w(n,o)?[n,o]:void 0}const vt=je(/^\d+$/);function bt(e){switch(e){case"false":return!1;case"true":return!0;case"falseStr":return"false";case"trueStr":return"true";case"null":return null;case"noopFunc":return()=>{};case"trueFunc":return()=>!0;case"falseFunc":return()=>!1;case"emptyArray":return[];case"emptyObj":return{};case"undefined":return;case"":return e;default:return vt.test(e)?Ke(e):e}}function Et(e,t){if(!e||!e.length)return!0;const n=Xe(),r=new nt(n),o=new URL(self.location.href);o.hash="";const s=/(.*?@)?(\S+)(:\d+):\d+\)?$/,i=[];for(let e of r.stack.split(/[\n\r]+/)){if(je(e).includes(n))continue;e=je(e).trim();const t=je(s).exec(e);if(null===t)continue;let r=t[2];je(r).startsWith("(")&&(r=je(r).slice(1)),r===o.href?r="inlineScript":je(r).startsWith("<anonymous>")&&(r="injectedScript");let a=t[1]?je(t[1]).slice(0,-1):je(e).slice(0,je(t).index).trim();je(a).startsWith("at")&&(a=je(a).slice(2).trim());let c=t[3];je(i).push(" "+`${a} ${r}${c}:1`.trim())}i[0]="stackDepth:"+(i.length-1);const a=je(i).join("\n");for(let n of e){if(Je(n).test(a))return t("info",`Found needle in stack trace: ${n}`),!0}return t("info",`Stack trace does not match any needle. Stack trace: ${a}`),!1}new rt;let{HTMLScriptElement:$t,Object:St,ReferenceError:Tt}=je(window),xt=St.getPrototypeOf($t);const{Error:Rt,Object:kt,Array:At,Map:jt}=je(window);let Lt=null;function Mt(e,t,n){let r=e;for(const e of n){if(!r||!w(r,e))return!1;r=r[e]}if("string"==typeof r||"number"==typeof r){const e=r.toString();return t.test(e)}return!1}const{Array:Pt,Blob:Ot,Error:Nt,Object:Ct,Reflect:It}=je(window),Wt=[];let{Error:Ft,URL:Dt}=je(window),{cookie:Ht}=Me(document);const{Map:zt,Object:Bt,Reflect:Ut,WeakMap:qt}=je(window),Vt=window.EventTarget.prototype.addEventListener,Gt=window.EventTarget.prototype.removeEventListener,Jt=new qt;let Xt=[];let{console:_t,document:Kt,getComputedStyle:Qt,isExtensionContext:Yt,variables:Zt,Array:en,MutationObserver:tn,Object:nn,DOMMatrix:rn,XPathEvaluator:on,XPathExpression:sn,XPathResult:an}=je(window);const{querySelectorAll:cn}=Kt,ln=cn&&s(cn,Kt);function pn(e,t=!1){return dn(e,ln.bind(Kt),Kt,t)}function un(e,t,n,r){const o=t.getAttribute("xlink:href")||t.getAttribute("href");if(o){const i=ln(o)[0];if(!i&&De())return _t.log("No elements found matching",o),!1;if(!(s=e)||0===s.length||s.every((e=>""===e.trim()))){const e=r.length>0?r:[];return n.push({element:i,rootParents:[...e,t]}),!1}const a=i.querySelectorAll.bind(i);return{nextBoundElement:i,nestedSelectorsString:e.join("^^"),next$$:a}}var s}function fn(e,t){const n=function(e,t=!1){try{const n=navigator.userAgent.includes("Firefox")?e.openOrClosedShadowRoot:browser.dom.openOrClosedShadowRoot(e);return null===n&&De()&&!t&&_t.log("Shadow root not found or not added in element yet",e),n}catch(n){return De()&&!t&&_t.log("Error while accessing shadow root",e,n),null}}(t);if(n){const{querySelectorAll:r}=n,o=r&&s(r,n).bind(n);return{nextBoundElement:t,nestedSelectorsString:":host "+e.join("^^"),next$$:o}}return!1}function dn(e,t,n,r,o=[]){if(e.includes("^^")){const[s,i,...a]=e.split("^^");let c,l;switch(i){case"svg":l=un;break;case"sh":l=fn;break;default:return De()&&_t.log(i," is not supported. Supported commands are: \n^^sh^^\n^^svg^^"),[]}c=""===s.trim()?[n]:t(s);const p=[];for(const e of c){const t=l(a,e,p,o);if(!t)continue;const{next$$:n,nestedSelectorsString:s,nextBoundElement:i}=t,c=dn(s,n,i,r,[...o,e]);c&&p.push(...c)}return p}const s=t(e);return r?[...s].map((e=>({element:e,rootParents:o.length>0?o:[]}))):s}const{assign:hn,setPrototypeOf:gn}=nn;class yn extends sn{evaluate(...e){return gn(o(super.evaluate,this,e),an.prototype)}}class mn extends on{createExpression(...e){return gn(o(super.createExpression,this,e),yn.prototype)}}function wn(e){if(Zt.hidden.has(e))return!1;!function(e){Yt&&"function"==typeof checkElement&&checkElement(e)}(e),Zt.hidden.add(e);let{style:t}=je(e),n=je(t,"CSSStyleDeclaration"),r=je([]);const o=$();let{debugCSSProperties:s}=o;for(let[e,t]of s||[["display","none"]])n.setProperty(e,t,"important"),r.push([e,n.getPropertyValue(e)]);return new tn((()=>{for(let[e,t]of r){let r=n.getPropertyValue(e),o=n.getPropertyPriority(e);r==t&&"important"==o||n.setProperty(e,t,"important")}})).observe(e,{attributes:!0,attributeFilter:["style"]}),!0}function vn(e){let t=e;if(t.startsWith("xpath(")&&t.endsWith(")")){let t=function(e){let t=e;if(t.startsWith("xpath(")&&t.endsWith(")")){let e=t.slice(6,-1),n=(new mn).createExpression(e,null),r=an.ORDERED_NODE_SNAPSHOT_TYPE;return e=>{if(!e)return;let t=n.evaluate(Kt,r,null),{snapshotLength:o}=t;for(let n=0;n<o;n++)e(t.snapshotItem(n))}}return t=>pn(e).forEach(t)}(e);return()=>{let e=je([]);return t((t=>e.push(t))),e}}return()=>en.from(pn(e))}let{ELEMENT_NODE:bn,TEXT_NODE:En,prototype:$n}=Node,{prototype:Sn}=Element,{prototype:Tn}=HTMLElement,{console:xn,variables:Rn,DOMParser:kn,Error:An,MutationObserver:jn,Object:Ln,ReferenceError:Mn}=je(window),{getOwnPropertyDescriptor:Pn}=Ln;const{CanvasRenderingContext2D:On,document:Nn,Map:Cn,MutationObserver:In,Object:Wn,Set:Fn,WeakSet:Dn}=je(window);let Hn,zn=new Fn,Bn=new Dn;function Un(e,t,n,r){const o=je(e).closest(t.selector);o&&!Bn.has(o)?(wn(o),Bn.add(o),Ue("hide-if-canvas-contains")("success","Matched: ",o,`\nFILTER: hide-if-canvas-contains ${t.formattedArguments}`)):function(e,t,n,r){zn.add({canvasElement:e,rule:t,functionName:n,text:r})}(e,t,n,r)}je(window);const{Map:qn,MutationObserver:Vn,Object:Gn,Set:Jn,WeakSet:Xn}=je(window);let _n=Element.prototype,{attachShadow:Kn}=_n,Qn=new Xn,Yn=new qn,Zn=null;const{Array:er,Error:tr,JSON:nr,Map:rr,Object:or,Response:sr}=je(window);let ir=null;let{Array:ar,Error:cr,JSON:lr,Map:pr,Object:ur,Response:fr}=je(window),dr=null;const{Error:hr,Object:gr,Map:yr}=je(window);let mr=null;function wr(e,t,n){if(!n.length){if("string"==typeof e||"number"==typeof e){const n=e.toString();return t.test(n)}return!1}let r=e;for(const e of n){if(!r||!w(r,e))return!1;r=r[e]}if("string"==typeof r||"number"==typeof r){const e=r.toString();return t.test(e)}return!1}let{Error:vr}=je(window);const{Array:br,addEventListener:Er,Error:$r,Object:Sr,Reflect:Tr,Set:xr,WeakSet:Rr}=je(window),kr=new Rr,Ar=new br,jr=new xr;let{Error:Lr,Map:Mr,Object:Pr,console:Or}=je(window),{toString:Nr}=Function.prototype,Cr=EventTarget.prototype,{addEventListener:Ir}=Cr,Wr=null;let{fetch:Fr}=je(window),Dr=!1;const Hr=[],zr=[],Br=()=>{Dr||(window.fetch=l(Fr,((...e)=>{let[t]=e;if(Hr.length>0&&"string"==typeof t){let n;try{n=new URL(t)}catch(e){if(!(e instanceof TypeError))throw e;n=new URL(t,je(document).location)}Hr.forEach((e=>e(n))),e[0]=n.href}return o(Fr,self,e).then((e=>{let t=e;return zr.forEach((e=>{t=e(t)})),t}))})),Dr=!0)};let Ur,{Map:qr,Object:Vr,RegExp:Gr,Response:Jr}=je(window);const{Error:Xr,Object:_r,atob:Kr,btoa:Qr,RegExp:Yr}=je(window);let Zr,eo,{RegExp:to,XMLHttpRequest:no,WeakMap:ro}=je(window);let oo,{delete:so,has:io}=c(URLSearchParams.prototype);const ao={"abort-current-inline-script":function(e,t=null){const n=_e(arguments),r=Ue("abort-current-inline-script"),{mark:o,end:s}=We("abort-current-inline-script"),a=t?Je(t):null,c=Xe(),l=je(document).currentScript;let p=window;const u=je(e).split("."),f=je(u).pop();for(let e of je(u))if(p=p[e],!p||"object"!=typeof p&&"function"!=typeof p)return void r("warn",u," is not found");const{get:d,set:h}=St.getOwnPropertyDescriptor(p,f)||{};let g=p[f];void 0===g&&r("warn","The property",f,"doesn't exist yet. Check typos.");const y=()=>{const e=je(document).currentScript;if(e instanceof xt&&""==je(e,"HTMLScriptElement").src&&e!=l&&(!a||a.test(je(e).textContent)))throw r("success",u," is aborted \n",e,"\nFILTER: abort-current-inline-script",n),new Tt(c)},m={get(){return y(),d?i(d,this):g},set(e){y(),h?i(h,this,e):g=e}};o(),ft(p,f,m),s(),dt(c)},"abort-on-iframe-property-read":function(...e){const{mark:t,end:n}=We("abort-on-iframe-property-read");t(),yt(e,!0,!1),n()},"abort-on-iframe-property-write":function(...e){const{mark:t,end:n}=We("abort-on-iframe-property-write");t(),yt(e,!1,!0),n()},"abort-on-property-read":function(e,t){const n=!("false"===t),r=_e(arguments),{mark:o,end:s}=We("abort-on-property-read");o(),ht("abort-on-property-read",window,e,r,n),s()},"abort-on-property-write":function(e,t){const n=_e(arguments),{mark:r,end:o}=We("abort-on-property-write"),s=!("false"===t);r(),gt("abort-on-property-write",window,e,n,s),o()},"array-override":function(e,t,n="false",r,s){if(!e)throw new Rt("[array-override snippet]: Missing method to override.");if(!t)throw new Rt("[array-override snippet]: Missing needle.");Lt||(Lt=new jt);let i=Ue("array-override");const{mark:a,end:c}=We("array-override"),p=_e(arguments);if("push"!==e||Lt.has("push"))if("includes"!==e||Lt.has("includes")){if("forEach"===e&&!Lt.has("forEach")){a();const{forEach:e}=At.prototype;Lt.set("forEach",je([])),kt.defineProperty(window.Array.prototype,"forEach",{value:l(e,(function(t,n){const r=Lt.get("forEach");return o(e,this,[function(e,s,a){for(const{needleRegex:t,pathSegments:n,stackNeedles:o}of r)if(n.length||"string"!=typeof e&&"number"!=typeof e){if(n.length&&"object"==typeof e&&null!==e&&Mt(e,t,n)&&Et(o,i))return void i("success",`Array.forEach skipped callback for object containing needle: ${t}\nFILTER: array-override ${p}`)}else{const n=e.toString();if(n.match&&n.match(t)&&Et(o,i))return void i("success",`Array.forEach skipped callback for item matching needle: ${t}\nFILTER: array-override ${p}`)}return o(t,n||this,[e,s,a])},n])}))}),i("info","Wrapped Array.prototype.forEach"),c()}}else{a();const{includes:e}=At.prototype;Lt.set("includes",je([])),kt.defineProperty(window.Array.prototype,"includes",{value:l(e,(function(t){const n=Lt.get("includes");for(const{needleRegex:e,retVal:r,pathSegments:o,stackNeedles:s}of n)if(o.length||"string"!=typeof t&&"number"!=typeof t){if(o.length&&"object"==typeof t&&null!==t&&Mt(t,e,o)&&Et(s,i))return i("success",`Array.includes returned ${r} for object containing ${e}\nFILTER: array-override ${p}`),r}else if(t.toString().match&&t.toString().match(e)&&Et(s,i))return i("success",`Array.includes returned ${r} for ${e}\nFILTER: array-override ${p}`),r;return o(e,this,arguments)}))}),i("info","Wrapped Array.prototype.includes"),c()}else{a();const{push:e}=At.prototype;Lt.set("push",je([])),kt.defineProperty(window.Array.prototype,"push",{value:l(e,(function(t){const n=Lt.get("push");for(const{needleRegex:e,pathSegments:r,stackNeedles:o}of n)if(r.length||"string"!=typeof t&&"number"!=typeof t){if(r.length&&"object"==typeof t&&null!==t&&Mt(t,e,r)&&Et(o,i))return void i("success",`Array.push is ignored for object containing needle: ${e}\nFILTER: array-override ${p}`)}else{const n=t.toString();if(n.match&&n.match(e)&&Et(o,i))return void i("success",`Array.push is ignored for needle: ${e}\nFILTER: array-override ${p}`)}return o(e,this,arguments)}))}),i("info","Wrapped Array.prototype.push"),c()}const u=Je(t);let f=[];r&&(f=r.split("."));let d=[];s&&(d=s.split(",").map((e=>e.trim())));const h=Lt.get(e),g="true"===n;h.push({needleRegex:u,retVal:g,pathSegments:f,stackNeedles:d}),Lt.set(e,h)},"blob-override":function(e,t="",n=null){if(!e)throw new Nt("[blob-override snippet]: Missing parameter search.");const r=Ue("blob-override"),o=_e(arguments),{mark:s,end:i}=We("blob-override");if(s(),Wt.push({match:Je(e),replaceWith:t,needle:n?Je(n):null,formattedArgs:o}),Wt.length>1)return;const a=Ot;function c(e,t={}){if(Pt.isArray(e)){let t=je(e).join("");for(const e of je(Wt))e.needle&&!e.needle.test(t)||!e.match.test(t)||(t=t.replace(e.match,e.replaceWith),r("success",`Replaced: ${e.match} → ${e.replaceWith},\nFILTER: blob-override ${e.formattedArgs}`));e=[t]}const n=It.construct(a,[e,t]);return Ct.setPrototypeOf(n,c.prototype),n}c.prototype=a.prototype,Ct.setPrototypeOf(c,a),window.Blob=c,r("info","Wrapped Blob constructor in context "),i()},"cookie-remover":function(e,t=!1){if(!e)throw new Ft("[cookie-remover snippet]: No cookie to remove.");const n=_e(arguments);let r=Ue("cookie-remover");const{mark:o,end:s}=We("cookie-remover");let i=Je(e);if(!je(/^http|^about/).test(location.protocol))return void r("warn","Snippet only works for http or https and about.");function a(){return je(Ht()).split(";").filter((e=>i.test(je(e).split("=")[0])))}const c=()=>{r("info","Parsing cookies for matches"),o();for(const e of je(a())){let t=je(location.hostname);!t&&je(location.ancestorOrigins)&&je(location.ancestorOrigins[0])&&(t=new Dt(je(location.ancestorOrigins[0])).hostname);const o=je(e).split("=")[0],s="expires=Thu, 01 Jan 1970 00:00:00 GMT",i="path=/",a=t.split(".");for(let e=a.length;e>0;e--){const t=a.slice(a.length-e).join(".");Ht(`${je(o).trim()}=;${s};${i};domain=${t}`),Ht(`${je(o).trim()}=;${s};${i};domain=.${t}`),r("success",`Set expiration date on ${o}`,"\nFILTER: cookie-remover",n)}}s()};if(c(),t){let e=a();setInterval((()=>{let t=a();if(t!==e)try{c()}finally{e=t}}),1e3)}},profile:function(){Ie=!1},debug:function(){Fe=!0},"event-override":function(e,t,n=null){const r=_e(arguments),s={eventType:e,mode:t,needle:n?Je(n):null,formattedArgs:r};if(Xt.includes(s)||Xt.push(s),Xt.length>1)return;let a=Ue("[event-override]");const{mark:c,end:p}=We("event-override"),u=Bt.getOwnPropertyDescriptor(window.EventTarget.prototype,"addEventListener");u.configurable&&Bt.defineProperty(window.EventTarget.prototype,"addEventListener",{...u,value:l(Vt,(function(e,t,n){c();const r=Xt.filter((t=>t.eventType===e));if(!r.length||e!==r[0].eventType)return p(),o(Vt,this,arguments);const s=r.find((e=>"disable"===e.mode&&(!e.needle||e.needle.test(t.toString()))));if(s)return a("success",`Disabling ${s.eventType} event, \nFILTER: event-override ${s.formattedArgs}`),void p();const l=r.filter((e=>"trusted"===e.mode&&(!e.needle||e.needle.test(t.toString()))));if("function"!=typeof t&&(!t||"function"!=typeof t.handleEvent)||!l.length||e!==l[0].eventType)return p(),o(Vt,this,arguments);const u=function(e){const n=new Proxy(e,{get(t,n){if("isTrusted"===n)return a("success",`Providing trusted value for ${e.type} event`),!0;const r=Ut.get(t,n);return"function"==typeof r?function(...e){return o(r,t,e)}:r}});return"function"==typeof t?i(t,this,n):i(t.handleEvent,t,n)};return u.originalListener=t,Jt.has(t)||Jt.set(t,new zt),Jt.get(t).set(e,u),a("info",`\nWrapping event listener for ${e}`),p(),o(Vt,this,[e,u,n])}))});const f=Bt.getOwnPropertyDescriptor(window.EventTarget.prototype,"removeEventListener");f.configurable&&Bt.defineProperty(window.EventTarget.prototype,"removeEventListener",{...f,value:l(Gt,(function(e,t,n){if(t&&Jt.has(t)&&Jt.get(t).has(e)){const r=Jt.get(t).get(e);return Jt.get(t).delete(e),o(Gt,this,[e,r,n])}return o(Gt,this,arguments)}))}),a("info","Initialized event-override snippet")},"freeze-element":function(e,t="",...n){let r,s,a=!1,c=!1,l=je(n).filter((e=>!h(e))),p=je(n).filter((e=>h(e))).map(Je),u=Xe(),f=vn(e);!function(){let n=je(t).split("+");1===n.length&&""===n[0]&&(n=[]);for(let t of n)switch(t){case"subtree":a=!0;break;case"abort":c=!0;break;default:throw new An("[freeze] Unknown option passed to the snippet. [selector]: "+e+" [option]: "+t)}}();let d={selector:e,shouldAbort:c,rid:u,exceptionSelectors:l,regexExceptions:p,changeId:0};function h(e){return e.length>=2&&"/"==e[0]&&"/"==e[e.length-1]}function g(){s=f(),y(s,!1)}function y(e,t=!0){for(let n of e)Rn.frozen.has(n)||(Rn.frozen.set(n,d),!t&&a&&new jn((e=>{for(let t of je(e))y(je(t,"MutationRecord").addedNodes)})).observe(n,{childList:!0,subtree:!0}),a&&je(n).nodeType===bn&&y(je(n).childNodes))}function m(e,...t){Be(`[freeze][${e}] `,...t)}function w(e,t,n,r){let o=r.selector,s=r.changeId,i="string"==typeof e,a=r.shouldAbort?"aborting":"watching";switch(xn.groupCollapsed(`[freeze][${s}] ${a}: ${o}`),n){case"appendChild":case"append":case"prepend":case"insertBefore":case"replaceChild":case"insertAdjacentElement":case"insertAdjacentHTML":case"insertAdjacentText":case"innerHTML":case"outerHTML":m(s,i?"text: ":"node: ",e),m(s,"added to node: ",t);break;case"replaceWith":case"after":case"before":m(s,i?"text: ":"node: ",e),m(s,"added to node: ",je(t).parentNode);break;case"textContent":case"innerText":case"nodeValue":m(s,"content of node: ",t),m(s,"changed to: ",e)}m(s,`using the function "${n}"`),xn.groupEnd(),r.changeId++}function v(e,t){if(t)for(let n of t)if(n.test(e))return!0;return!1}function b(e){throw new Mn(e)}function E(e,t,n,r){let o=new kn,{body:s}=je(o.parseFromString(e,"text/html")),i=$(je(s).childNodes,t,n,r);return je(i).map((e=>{switch(je(e).nodeType){case bn:return je(e).outerHTML;case En:return je(e).textContent;default:return""}})).join("")}function $(e,t,n,r){let o=je([]);for(let s of e)S(s,t,n,r)&&o.push(s);return o}function S(e,t,n,r){let o=r.shouldAbort,s=r.regexExceptions,i=r.exceptionSelectors,a=r.rid;if("string"==typeof e){let i=e;return!!v(i,s)||(De()&&w(i,t,n,r),o&&b(a),De())}let c=e;switch(je(c).nodeType){case bn:return!!function(e,t){if(t){let n=je(e);for(let e of t)if(n.matches(e))return!0}return!1}(c,i)||(o&&(De()&&w(c,t,n,r),b(a)),!!De()&&(wn(c),w(c,t,n,r),!0));case En:return!!v(je(c).textContent,s)||(De()&&w(c,t,n,r),o&&b(a),!1);default:return!0}}function T(e,t,n,r){let s=Pn(e,t)||{},a=s.get&&i(s.get,e)||s.value;if(a)return{get:()=>function(...e){if(n(this)){let n=r(this);if(n){let r=e[0];if(!S(r,this,t,n))return r}}return o(a,this,e)}}}function x(e,t,n,r){let s=Pn(e,t)||{},a=s.get&&i(s.get,e)||s.value;if(a)return{get:()=>function(...e){if(!n(this))return o(a,this,e);let s=r(this);if(!s)return o(a,this,e);let i=$(e,this,t,s);return i.length>0?o(a,this,i):void 0}}}function R(e,t,n,r){let s=Pn(e,t)||{},a=s.get&&i(s.get,e)||s.value;if(a)return{get:()=>function(...e){let[s,c]=e,l="afterbegin"===s||"beforeend"===s;if(n(this,l)){let e=r(this,l);if(e){let n,r=l?this:je(this).parentNode;switch(t){case"insertAdjacentElement":if(!S(c,r,t,e))return c;break;case"insertAdjacentHTML":return n=E(c,r,t,e),n?i(a,this,s,n):void 0;case"insertAdjacentText":if(!S(c,r,t,e))return}}}return o(a,this,e)}}}function k(e,t,n,r){let o=Pn(e,t)||{},{set:s}=o;if(s)return{set(e){if(!n(this))return i(s,this,e);let o=r(this);if(!o)return i(s,this,e);let a=E(e,this,t,o);return a?i(s,this,a):void 0}}}function A(e,t,n,r){let o=Pn(e,t)||{},{set:s}=o;if(s)return{set(e){if(!n(this))return i(s,this,e);let o=r(this);return o?S(e,this,t,o)?i(s,this,e):void 0:i(s,this,e)}}}Rn.frozen.has(document)||(Rn.frozen.set(document,!0),function(){let e;function t(e){return e&&Rn.frozen.has(e)}function n(e){try{return e&&(Rn.frozen.has(e)||Rn.frozen.has(je(e).parentNode))}catch(e){return!1}}function r(e,t){try{return e&&(Rn.frozen.has(e)&&t||Rn.frozen.has(je(e).parentNode)&&!t)}catch(e){return!1}}function o(e){return Rn.frozen.get(e)}function s(e){try{if(Rn.frozen.has(e))return Rn.frozen.get(e);let t=je(e).parentNode;return Rn.frozen.get(t)}catch(e){}}function i(e,t){try{if(Rn.frozen.has(e)&&t)return Rn.frozen.get(e);let n=je(e).parentNode;return Rn.frozen.get(n)}catch(e){}}e=T($n,"appendChild",t,o),ft($n,"appendChild",e),e=T($n,"insertBefore",t,o),ft($n,"insertBefore",e),e=T($n,"replaceChild",t,o),ft($n,"replaceChild",e),e=x(Sn,"append",t,o),ft(Sn,"append",e),e=x(Sn,"prepend",t,o),ft(Sn,"prepend",e),e=x(Sn,"replaceWith",n,s),ft(Sn,"replaceWith",e),e=x(Sn,"after",n,s),ft(Sn,"after",e),e=x(Sn,"before",n,s),ft(Sn,"before",e),e=R(Sn,"insertAdjacentElement",r,i),ft(Sn,"insertAdjacentElement",e),e=R(Sn,"insertAdjacentHTML",r,i),ft(Sn,"insertAdjacentHTML",e),e=R(Sn,"insertAdjacentText",r,i),ft(Sn,"insertAdjacentText",e),e=k(Sn,"innerHTML",t,o),ft(Sn,"innerHTML",e),e=k(Sn,"outerHTML",n,s),ft(Sn,"outerHTML",e),e=A($n,"textContent",t,o),ft($n,"textContent",e),e=A(Tn,"innerText",t,o),ft(Tn,"innerText",e),e=A($n,"nodeValue",t,o),ft($n,"nodeValue",e)}()),r=new jn(g),r.observe(document,{childList:!0,subtree:!0}),g()},"hide-if-canvas-contains":function(e,t="canvas"){const n=Ue("hide-if-canvas-contains"),r=_e(arguments),{mark:s,end:i}=We("hide-if-canvas-contains");if(!e)return void n("error","The parameter 'search' is required");if(!Hn){s();const c=On.prototype;function p(e){const t=c[e];Wn.defineProperty(window.CanvasRenderingContext2D.prototype,e,{value:l(t,(function(n,...r){for(const[t,r]of Hn)t.test(n)&&Un(this.canvas,r,e,n);return o(t,this,[n,...r])}))})}function u(){const e=c.drawImage;Wn.defineProperty(window.CanvasRenderingContext2D.prototype,"drawImage",{value:l(e,(function(t,...r){if(n("info","drawImage called with arguments:",t,...r),t&&"string"==typeof t.src&&t.src)for(const[e,n]of Hn)e.test(t.src)&&Un(this.canvas,n,"drawImage",t.src);return o(e,this,[t,...r])}))})}n("info","CanvasRenderingContext2D proxied"),p("fillText"),p("strokeText"),u(),Hn=new Cn;new In((e=>{for(let t of je(e))"childList"===t.type&&zn.forEach((e=>{const t=je(e.canvasElement).closest(e.rule.selector);t&&!Bn.has(t)&&(wn(t),Bn.add(t),zn.delete(e),Ue("hide-if-canvas-contains")("success","Matched: ",t,`\nFILTER: hide-if-canvas-contains ${e.rule.formattedArguments}`))}))})).observe(Nn,{childList:!0,subtree:!0}),i()}const a=Je(e);Hn.set(a,{selector:t,formattedArguments:r})},"hide-if-shadow-contains":function(e,t="*"){const n=_e(arguments);let r=`${e}\\${t}`;Yn.has(r)||Yn.set(r,[Je(e),t,ze],n);const s=Ue("hide-if-shadow-contains"),{mark:i,end:a}=We("hide-if-shadow-contains");Zn||(Zn=new Vn((e=>{i();let t=new Jn;for(let{target:r}of je(e)){let e=je(r).parentNode;for(;e;)[r,e]=[e,je(r).parentNode];if(!Qn.has(r)&&!t.has(r)){t.add(r);for(let[e,t,o]of Yn.values())if(e.test(je(r).textContent)){let e=je(r.host).closest(t);e&&(o(),je(r).appendChild(document.createElement("style")).textContent=":host {display: none !important}",wn(e),Qn.add(r),s("success","Hiding: ",e,`\nFILTER: hide-if-shadow-contains ${n}`)),a()}}}})),Gn.defineProperty(_n,"attachShadow",{value:l(Kn,(function(){let e=o(Kn,this,arguments);return s("info","attachShadow is called for: ",e),Zn.observe(e,{childList:!0,characterData:!0,subtree:!0}),e}))}))},"json-override":function(e,t,n="",r=""){if(!e)throw new tr("[json-override snippet]: Missing paths to override.");if(void 0===t)throw new tr("[json-override snippet]: No value to override with.");if(!ir){let i=Ue("json-override");const{mark:a,end:c}=We("json-override");function p(e,t){for(let{formattedArgs:n,prune:r,needle:o,filter:s,value:i}of ir.values())if(!s||s.test(t)){if(je(o).some((t=>!wt(e,t))))return e;for(let t of r)t.includes("{}")||t.includes("[]")?u(e,t,i,n):f(e,t,i,n)}return e}function u(e,t,n,r){let o=je(t).split("."),s=e;for(let e=0;e<o.length;e++){let a=o[e];if("[]"===a)return void(er.isArray(s)&&(i("info",`Iterating over array at: ${a}`),je(s).forEach((t=>{null!=t&&u(t,o.slice(e+1).join("."),n,r)}))));if("{}"===a)return void(s&&"object"==typeof s&&(i("info",`Iterating over object at: ${a}`),or.keys(s).forEach((t=>{let i=s[t];null!=i&&u(i,o.slice(e+1).join("."),n,r)}))));if(!s||"object"!=typeof s||!w(s,a))return;e===o.length-1?(i("success",`Found ${t}, replaced it with ${n}`,`\nFILTER: json-override ${r}`),s[a]=bt(n)):s=s[a]}}function f(e,t,n,r){let o=wt(e,t);void 0!==o&&(i("success",`Found ${t}, replaced it with ${n}`,`\nFILTER: json-override ${r}`),o[0][o[1]]=bt(n))}a();let{parse:d}=nr;ir=new rr,or.defineProperty(window.JSON,"parse",{value:l(d,(function(e){return p(o(d,this,arguments),e)}))}),i("info","Wrapped JSON.parse for override");let{json:h}=sr.prototype;or.defineProperty(window.Response.prototype,"json",{value:l(h,(function(e){return o(h,this,arguments).then((t=>p(t,e)))}))}),i("info","Wrapped Response.json for override"),c()}const s=_e(arguments);ir.set(e,{formattedArgs:s,prune:je(e).split(/ +/),needle:n.length?je(n).split(/ +/):[],filter:r?Je(r):null,value:t})},"json-prune":function(e,t="",n=""){if(!e)throw new cr("Missing paths to prune");if(!dr){let s=Ue("json-prune");const{mark:i,end:a}=We("json-prune");function c(e){for(let{prune:t,needle:n,stackNeedle:r,formattedArgs:o}of dr.values()){if(je(n).length>0&&je(n).some((t=>!wt(e,t))))return e;if(je(r)&&je(r).length>0&&!Et(r,s))return e;for(let n of t)n.includes("{}")||n.includes("[]")||n.includes("{-}")||n.includes("[-]")?p(e,n,o):f(e,n,o)}return e}function p(e,t,n){let r=je(t).split("."),o=e;for(let e=0;e<r.length;e++){let i=r[e];if("[]"===i)return void(ar.isArray(o)&&(s("info",`Iterating over array at: ${i}`),je(o).forEach((t=>p(t,r.slice(e+1).join("."),n)))));if("[-]"===i){if(ar.isArray(o)){s("info",`Iterating over array with element removal at: ${i}`);let t=r.slice(e+1).join("."),a=[];je(o).forEach(((e,n)=>{u(e,t)&&a.push(n)}));for(let e=a.length-1;e>=0;e--)s("success",`Found element at index ${a[e]} matching ${t} and removed entire element, \nFILTER: json-prune ${n}`),o.splice(a[e],1)}return}if("{}"===i)return void("object"==typeof o&&null!==o&&(s("info",`Iterating over object at: ${i}`),ur.keys(o).forEach((t=>p(o[t],r.slice(e+1).join("."),n)))));if("{-}"===i){if("object"==typeof o&&null!==o){s("info",`Iterating over object with element removal at: ${i}`);let t=r.slice(e+1).join("."),a=[];ur.keys(o).forEach((e=>{u(o[e],t)&&a.push(e)})),a.forEach((e=>{s("success",`Found object key ${e} matching ${t} and removed entire element, \nFILTER: json-prune ${n}`),delete o[e]}))}return}if(!o||"object"!=typeof o||!w(o,i))return;e===r.length-1?(s("success",`Found ${t} and deleted, \nFILTER: json-prune ${n}`),delete o[i]):o=o[i]}}function u(e,t){if(!t||""===t)return!0;let n=je(t).split("."),r=e;for(let e=0;e<n.length;e++){let t=n[e];if("[]"===t)return!!ar.isArray(r)&&je(r).some((t=>u(t,n.slice(e+1).join("."))));if("{}"===t)return"object"==typeof r&&null!==r&&ur.keys(r).some((t=>u(r[t],n.slice(e+1).join("."))));if(!r||"object"!=typeof r||!w(r,t))return!1;if(e===n.length-1)return!0;r=r[t]}return!1}function f(e,t,n){let r=wt(e,t);void 0!==r&&(s("success",`Found ${t} and deleted`,`\nFILTER: json-prune ${n}`),delete r[0][r[1]])}i();let{parse:d}=lr;dr=new pr,ur.defineProperty(window.JSON,"parse",{value:l(d,(function(){return c(o(d,this,arguments))}))}),s("info","Wrapped JSON.parse for prune");let{json:h}=fr.prototype;ur.defineProperty(window.Response.prototype,"json",{value:l(h,(function(){return o(h,this,arguments).then((e=>c(e)))}))}),s("info","Wrapped Response.json for prune"),a()}const r=_e(arguments);dr.set(e,{formattedArgs:r,prune:je(e).split(/ +/),needle:t.length?je(t).split(/ +/):[],stackNeedle:n.length?je(n).split(/ +/):[]})},"map-override":function(e,t,n="",r,s){if(!e)throw new hr("[map-override snippet]: Missing method to override.");if(!t)throw new hr("[map-override snippet]: Missing needle.");mr||(mr=new yr);let a=Ue("map-override");const{mark:c,end:p}=We("map-override"),{set:u,get:f,has:d}=yr.prototype,h=_e(arguments);"set"!==e||mr.has("set")?"get"!==e||mr.has("get")?"has"!==e||mr.has("has")||(c(),i(u,mr,"has",je([])),gr.defineProperty(window.Map.prototype,"has",{value:l(d,(function(e){const t=i(f,mr,"has");for(const{needleRegex:n,retVal:r,stackNeedles:o}of t)if("string"==typeof e||"number"==typeof e){const t=e.toString();if(n.test(t)&&Et(o,a))return a("success",`Map.has returned ${r} for key: ${t}\nFILTER: map-override ${h}`),r}return o(d,this,arguments)}))}),a("info","Wrapped Map.prototype.has"),p()):(c(),i(u,mr,"get",je([])),gr.defineProperty(window.Map.prototype,"get",{value:l(f,(function(e){const t=i(f,mr,"get");for(const{needleRegex:n,retVal:r,stackNeedles:o}of t)if("string"==typeof e||"number"==typeof e){const t=e.toString();if(n.test(t)&&Et(o,a))return a("success",`Map.get returned ${r} for key: ${t}\nFILTER: map-override ${h}`),r}return o(f,this,arguments)}))}),a("info","Wrapped Map.prototype.get"),p()):(c(),i(u,mr,"set",je([])),gr.defineProperty(window.Map.prototype,"set",{value:l(u,(function(e,t){const n=i(f,mr,"set");for(const{needleRegex:e,pathSegments:r,stackNeedles:o}of n)if(wr(t,e,r)&&Et(o,a))return a("success",`Map.set is ignored for value matching needle: ${e}\nFILTER: map-override ${h}`),this;return o(u,this,arguments)}))}),a("info","Wrapped Map.prototype.set"),p());const g=Je(t);let y=[];r&&(y=r.split("."));let m=[];s&&(m=s.split(",").map((e=>e.trim())));const w=i(f,mr,e);let v;"get"===e?v=""===n?void 0:n:"has"===e&&(v="true"===n),w.push({needleRegex:g,retVal:v,pathSegments:y,stackNeedles:m}),i(u,mr,e,w)},"override-property-read":function(e,t,n){if(!e)throw new vr("[override-property-read snippet]: No property to override.");if(void 0===t)throw new vr("[override-property-read snippet]: No value to override with.");const r=_e(arguments);let o=Ue("override-property-read");const{mark:s,end:i}=We("override-property-read");let a=bt(t);o("info",`Overriding ${e}.`);const c=!("false"===n);s(),ft(window,e,{get:()=>(o("success",`${e} override done.`,"\nFILTER: override-property-read",r),a),set(){}},c),i()},"prevent-element-src-loading":function(e,t){if(!e||"string"!=typeof e)throw new $r("[prevent-element-src-loading snippet]: tagName param must be a string.");if(!t)throw new $r("[prevent-element-src-loading snippet]: Missing search parameter.");if(e=je(e).toString().toLowerCase(),!je(["script","img","iframe","link"]).includes(e))throw new $r("[prevent-element-src-loading snippet]: tagName parameter is incorrect.");const n={script:"data:text/javascript;base64,KCk9Pnt9",img:"data:image/gif;base64,R0lGODlhAQABAAAAACH5BAEKAAEALAAAAAABAAEAAAICTAEAOw==",iframe:"data:text/html;base64,PGRpdj48L2Rpdj4=",link:"data:text/plain;base64,"},r={script:window.HTMLScriptElement,img:window.HTMLImageElement,iframe:window.HTMLIFrameElement,link:window.HTMLLinkElement}[e],o="link"===e?"href":"src",s="onerror",i=Ue("[prevent-element-src-loading snippet]"),a=_e(arguments),{mark:c,end:l}=We("prevent-element-src-loading");c();const p=Je(t);if(Ar.push({tagName:e,searchRegex:p}),i("info",`Added filter rule\nFILTER: prevent-element-src-loading ${a}`),!jr.has(e)){jr.add(e);const t={apply:(e,t,r)=>{if(!r[0]||!r[1])return Tr.apply(e,t,r);const s=t.nodeName.toLowerCase(),a=r[0].toLowerCase(),c=r[1];return a===o&&Ar.some((e=>s===e.tagName&&e.searchRegex.test(c)))?(kr.add(t),i("success",`Replaced setAttribute for ${a}: ${c} → ${n[s]}`),Tr.apply(e,t,[a,n[s]])):Tr.apply(e,t,r)}};r.prototype.setAttribute=new Proxy(r.prototype.setAttribute,t),i("info","Wrapped setAttribute function");const s=Sr.getOwnPropertyDescriptor(r.prototype,o);if(!s)return;Sr.defineProperty(r.prototype,o,{enumerable:!0,configurable:!0,get(){return s.get.call(this)},set(e){const t=this.nodeName.toLowerCase();Ar.some((n=>t===n.tagName&&n.searchRegex.test(e)))?(kr.add(this),i("success",`Replaced in src/href setter ${e} → ${n[t]}`),s.set.call(this,n[t])):s.set.call(this,e)}}),i("info","Wrapped src/href property setter")}if(1===Ar.length){const e=Sr.getOwnPropertyDescriptor(HTMLElement.prototype,s);if(!e)return;Sr.defineProperty(HTMLElement.prototype,s,{enumerable:!0,configurable:!0,get(){return e.get.call(this)},set(t){kr.has(this)?(i("success",`Replaced in onerror setter ${t} → () => {}`),e.set.call(this,(()=>{}))):e.set.call(this,t)}}),i("info","Wrapped onerror property setter");const t={apply:(e,t,n)=>{if(!n[0]||!n[1]||!t)return Tr.apply(e,t,n);const r=n[0];return"function"==typeof t.getAttribute&&kr.has(t)&&"error"===r?(i("success",`Replaced error event handler on ${t} with () => {}`),Tr.apply(e,t,[r,()=>{}])):Tr.apply(e,t,n)}};EventTarget.prototype.addEventListener=new Proxy(EventTarget.prototype.addEventListener,t),i("info","Wrapped addEventListener");(()=>{Er("error",(e=>{const t=e.target;if(!t||!t.nodeName)return;const n=t.src||t.href,r=t.nodeName.toLowerCase();Ar.some((e=>r===e.tagName&&n&&e.searchRegex.test(n)))&&(t.onerror=()=>{})}),!0),i("info","Added event listener to defuse global errors")})()}l()},"prevent-listener":function(e,t,n){if(!e)throw new Lr("[prevent-listener snippet]: No event type.");if(!Wr){Wr=new Mr;let e=Ue("[prevent]");const{mark:t,end:n}=We("prevent-listener");Pr.defineProperty(Cr,"addEventListener",{value:l(Ir,(function(r,s){t();for(let{evt:t,handlers:n,selectors:o}of Wr.values()){if(!t.test(r))continue;let a=this instanceof Element;for(let l=0;l<n.length;l++){const p=n[l],u=o[l];if(!u||a&&je(this).matches(u)){if(p){const t=function(){try{const e=String("function"==typeof s?s:s.handleEvent);return p.test(e)}catch(t){return e("error","Error while trying to stringify listener: ",t),!1}};if(!function(){try{const e=i(Nr,"function"==typeof s?s:s.handleEvent);return p.test(e)}catch(t){return e("error","Error while trying to stringify listener: ",t),!1}}()&&!t())continue}return void(De()&&(Or.groupCollapsed("DEBUG [prevent] was successful",`\nFILTER: prevent-listener ${c}`),e("success",`type: ${r} matching ${t}`),e("success","handler:",s),p&&e("success",`matching ${p}`),u&&e("success","on element: ",this,` matching ${u}`),e("success","was prevented from being added"),Or.groupEnd()))}}}return n(),o(Ir,this,arguments)}))}),e("info","Wrapped addEventListener")}const r=_e(arguments);Wr.has(e)||Wr.set(e,{evt:Je(e),handlers:[],selectors:[],formattedArgs:r});let{handlers:s,selectors:a,formattedArgs:c}=Wr.get(e);s.push(t?Je(t):null),a.push(n)},"replace-fetch-response":function(e,t="",n=null){const r=_e(arguments),o=Ue("replace-fetch-response"),{mark:s,end:i}=We("replace-fetch-response");if(!e)return void o("error","The parameter 'search' is required");if(!Ur){const e=e=>{s();return je(e).clone().text().then((t=>{let n=je(t);for(const[e,{replacement:r,needle:s,formattedArgs:i}]of Ur){if(s){if(!Je(s).test(n)){De()&&(console.groupCollapsed(`DEBUG [replace-fetch-response] warn: '${s}' not found in fetch response`),o("warn",`${n}`),console.groupEnd());continue}De()&&(console.groupCollapsed(`DEBUG [replace-fetch-response] success: '${s}' found in fetch response`),o("info",`${n}`),console.groupEnd())}n=n.replace(e,r),De()&&n.toString()!==t.toString()&&(console.groupCollapsed(`DEBUG [replace-fetch-response] success: '${e}' replaced with '${r}' in fetch response`,`\nFILTER: replace-fetch-response ${i}`),o("success",`${n}`),console.groupEnd())}if(n.toString()===t.toString())return e;const r=new Jr(n.toString(),{status:e.status,statusText:e.statusText,headers:e.headers});return Vr.defineProperties(r,{ok:{value:e.ok},redirected:{value:e.redirected},type:{value:e.type},url:{value:e.url}}),i(),r}))};Ur=new qr,o("info","Network API proxied"),a=e,zr.push(a),Br()}var a;const c=Je(e),l=new Gr(c,"g");Ur.set(l,{replacement:t,needle:n,formattedArgs:r})},"replace-outbound-value":function(e,t="",n="",r="",s="",i=""){if(!e)throw new Xr("[replace-outbound-value snippet]: Missing method path.");let a=Ue("replace-outbound-value");const{mark:c,end:p}=We("replace-outbound-value");function u(e,t,n,r){if("base64"===r)try{if(function(e){try{if(""===e)return!1;const t=Kr(e),n=Qr(t),r=je(e).replace(/=+$/,"").toString();return je(n).replace(/=+$/,"").toString()===r}catch(e){return!1}}(e)){const r=Kr(e);a("info",`Decoded base64 content: ${r}`);const o=t?je(r).replace(t,n).toString():r;a("info",o!==r?`Modified decoded content: ${o}`:"Decoded content was not modified");const s=Qr(o);return a("info",`Re-encoded to base64: ${s}`),s}a("info",`Content is plain text: ${e}`);const r=t?je(e).replace(t,n).toString():e;a("info",r!==e?`Modified plain text content: ${r}`:"Plain text content was not modified");const o=Qr(r);return a("info",`Encoded to base64: ${o}`),o}catch(t){return a("info",`Error processing base64 content: ${t.message}`),e}return t?je(e).replace(t,n).toString():e}function f(e,t,n,r,o,s){const i=n?new Yr(Je(n),"g"):null;if(t.length&&"object"==typeof e&&null!==e){const c=n?function(e,t,n,r,o){if(!t.length)return e;let s=e;for(let n=0;n<t.length-1;n++){if(!s||"object"!=typeof s)return a("info",`Cannot navigate to path: property '${t[n]}' not found`),e;s=s[t[n]]}const i=t[t.length-1];if(!s||"object"!=typeof s||!(i in s))return a("info",`Target property '${i}' not found at path`),e;const c=s[i];if("string"!=typeof c)return a("info","Property at path is not a string: "+typeof c),e;const l=u(c,n,r,o);if(l!==c){const n=JSON.parse(JSON.stringify(e));let r=n;for(let e=0;e<t.length-1;e++)r=r[t[e]];return r[i]=l,a("info",`Replaced value at path '${t.join(".")}': '${c}' -> '${l}'`),n}return e}(e,t,i,r,o):e;return c!==e&&a("success",`Replaced outbound value\nFILTER: replace-outbound-value ${s}`),c}if("string"==typeof e){n||a("info",`Original text content: ${e}`);const t=n?u(e,i,r,o):e;return t!==e&&a("success",`Replaced outbound value: ${t} \nFILTER: replace-outbound-value ${s}`),t}return e}_e(arguments),c();const d=function(e,t){let n=e,r=je(t).split(".");for(let e=0;e<r.length-1;e++){let t=r[e];if(!n||"object"!=typeof n&&"function"!=typeof n)return{base:n,prop:t,remainingPath:r.slice(e).join("."),success:!1};n=n[t]}return{base:n,prop:r[r.length-1],success:!0}}(window,e);if(!d.success)return a("error",`Could not reach the end of the prop chain: ${e}. Remaining path: ${d.remainingPath}`),void p();const{base:h,prop:g}=d,y=h[g];if(!y||"function"!=typeof y)return a("error",`Could not retrieve the method: ${e}`),void p();let m=[];s&&(m=je(s).split("."));let w=[];i&&(w=je(i).split(",").map((e=>e.trim())));let v=!1;_r.defineProperty(h,g,{value:l(y,(function(){if(v)return o(y,this,arguments);v=!0;const e=o(y,this,arguments);if(w.length&&!Et(w,a))return v=!1,e;if(e&&"function"==typeof e.then)return a("info","Method returned a Promise, modifying resolved value"),v=!1,e.then((e=>{const o="object"==typeof e?JSON.stringify(e):e;return a("info",`Promise resolved with value: ${o}`),f(e,m,t,n,r,s)})).catch((e=>{throw a("info",`Promise rejected: ${e.message}`),e}));const i=f(e,m,t,n,r,s);return v=!1,i}))}),a("info",`Wrapped ${e}`),p()},"replace-xhr-response":function(e,t="",n=null){const r=_e(arguments),o=Ue("replace-xhr-response"),{mark:s,end:i}=We("replace-xhr-response");if(!e)return void o("error","The parameter 'pattern' is required");Zr||(Zr=new ro,eo=new Map,o("info","XMLHttpRequest proxied"),window.XMLHttpRequest=class extends no{open(e,t,...n){const r={method:e,url:t};return Zr.set(this,r),super.open(e,t,...n)}send(...e){return super.send(...e)}get response(){const e=super.response,t=Zr.get(this);if(void 0===t)return e;s();const n="string"==typeof e?e.length:void 0;if(t.lastResponseLength!==n&&(t.response=void 0,t.lastResponseLength=n),void 0!==t.response)return t.response;if("string"!=typeof e)return t.response=e;let r=e;for(const[t,{replacement:n,needle:s,formattedArgs:i}]of eo){if(s){if(!Je(s).test(r)){De()&&(console.groupCollapsed(`DEBUG [replace-xhr-response] warn: '${s}' not found in XHR response`),o("warn",r),console.groupEnd());continue}De()&&(console.groupCollapsed(`DEBUG [replace-xhr-response] success: '${s}' found in XHR response`),o("info",r),console.groupEnd())}r=je(r).replace(t,n).toString(),De()&&e.toString()!==r.toString()&&(console.groupCollapsed(`DEBUG [replace-xhr-response] success: '${t}' replaced with '${n}' in XHR response`,`\nFILTER: replace-xhr-response ${i}`),o("success",r),console.groupEnd())}return i(),t.response=r.toString()}get responseText(){const e=this.response;return"string"!=typeof e?super.responseText:e}});const a=Je(e),c=new to(a,"g");eo.set(c,{replacement:t,needle:n,formattedArgs:r})},"strip-fetch-query-parameter":function(e,t=null){const n=_e(arguments),r=Ue("strip-fetch-query-parameter"),{mark:o,end:s}=We("strip-fetch-query-parameter"),i=e=>{o();for(let[t,n]of oo.entries()){const{reg:o,args:s}=n;o&&!o.test(e)||io(e.searchParams,t)&&(r("success",`${t} has been stripped from url ${e}`,`\nFILTER: strip-fetch-query-parameter ${s}`),so(e.searchParams,t))}s()};var a;oo||(oo=new Map,a=i,Hr.push(a),Br()),oo.set(e,{reg:t&&Je(t),args:n})},trace:function(...e){o(Be,null,e)}};
const snippets=ao;
let context;
for (const [name, ...args] of filters) {
if (snippets.hasOwnProperty(name)) {
try { context = snippets[name].apply(context, args); }
catch (error) { console.error(error); }
}
}
context = void 0;
};
const graph = new Map([["abort-current-inline-script",null],["abort-on-iframe-property-read",null],["abort-on-iframe-property-write",null],["abort-on-property-read",null],["abort-on-property-write",null],["array-override",null],["blob-override",null],["cookie-remover",null],["profile",null],["debug",null],["event-override",null],["freeze-element",null],["hide-if-canvas-contains",null],["hide-if-shadow-contains",null],["json-override",null],["json-prune",null],["map-override",null],["override-property-read",null],["prevent-element-src-loading",null],["prevent-listener",null],["replace-fetch-response",null],["replace-outbound-value",null],["replace-xhr-response",null],["strip-fetch-query-parameter",null],["trace",null]]);
callback.get = snippet => graph.get(snippet);
callback.has = snippet => graph.has(snippet);
callback.getGraph = () => graph;
callback.setEnvironment = env => {
  if (typeof currentEnvironment !== "undefined")
    currentEnvironment = env;
};
callback.setDebugStyle = styles => {
  if (typeof currentEnvironment !== "undefined")
  {
    delete currentEnvironment.initial;
    currentEnvironment.debugCSSProperties = styles;
  }
    
};
callback.getEnvironment = () => currentEnvironment;
/* harmony default export */ const main = (callback);
;// ./src/content/shared/constants.js
/*
 * This file is part of eyeo's Web Extension Ad Blocking Toolkit (EWE),
 * Copyright (C) 2006-present eyeo GmbH
 *
 * EWE is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
 *
 * EWE is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with EWE.  If not, see <http://www.gnu.org/licenses/>.
 */

/**
 * Prefix that should be used for storage and synchronization to avoid conflicts
 * when multiple extensions are installed in the same session.
 *
 * !!! IMPORTANT - DO NOT CHANGE THIS VALUE !!!
 * This exact string "abp" is hardcoded in the build
 * configurations and is replaced during the build process with host-specific
 * values (e.g., "ab" for Adblock, "abp" for Adblock Plus).
 *
 * If you change this value, the build process will NOT replace it, and the
 * extension will fail to work properly due to namespace conflicts.
 *
 * Build configuration references:
 * - host/adblock/build/config/base.mjs (replacements.search)
 * - host/adblockplus/build/webext/config/base.mjs (replacements.search)
 *
 * @type {string}
 */
const HOST_PREFIX_TO_REPLACE = "abp";

/**
 * Dataset key used to exchange the communication channel name between content
 * scripts in different contexts (main world and isolated world)
 * @type {string}
 */
const COMMS_CHANNEL_DATASET_KEY = `${HOST_PREFIX_TO_REPLACE}FiltersChannel`;

/**
 * Event used to communicate between content script contexts
 * @type {string}
 */
const HANDSHAKE_EVENT_NAME = `${HOST_PREFIX_TO_REPLACE}-handshake`;

/**
 * Storage key used to cache the filters config in content scripts
 * @type {string}
 */
const CACHED_FILTERS_CONFIG_KEY = `${HOST_PREFIX_TO_REPLACE}-filters-config`;

/**
 * CSS properties applied to elements hidden in debug mode
 * @type {string[][]}
 */
const DEBUG_CSS_PROPERTIES = [
  ["background", "repeating-linear-gradient(to bottom, #e67370 0, #e67370 9px, white 9px, white 10px)"],
  ["outline", "solid red"]
];

;// ./src/content/main/shims/storage.js
/*
 * This file is part of eyeo's Web Extension Ad Blocking Toolkit (EWE),
 * Copyright (C) 2006-present eyeo GmbH
 *
 * EWE is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
 *
 * EWE is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with EWE.  If not, see <http://www.gnu.org/licenses/>.
 */

/* eslint-disable no-extend-native */

function shimStorage(CACHED_FILTERS_CONFIG_KEY) {
  // =================== Secured copies of native functions ====================
  // These are captured before page scripts run.
  // Used inside Proxy apply handlers which run after page scripts.
  const {parse: $JSONparse, stringify: $JSONstringify} = JSON;
  const {keys: $ObjectKeys} = Object;
  const {
    apply: $ReflectApply,
    ownKeys: $ReflectOwnKeys,
    get: $ReflectGet,
    set: $ReflectSet,
    has: $ReflectHas,
    getOwnPropertyDescriptor: $ReflectGetOwnPropertyDescriptor,
    defineProperty: $ReflectDefineProperty,
    deleteProperty: $ReflectDeleteProperty
  } = Reflect;
  const {filter: $ArrayFilter} = Array.prototype;
  const {get: $MapGet, set: $MapSet, has: $MapHas} = Map.prototype;
  const $String = String;

  // Helpers using secured copies
  const filter = (arr, fn) => $ReflectApply($ArrayFilter, arr, [fn]);
  const mapGet = (map, key) => $ReflectApply($MapGet, map, [key]);
  const mapSet = (map, key, val) => $ReflectApply($MapSet, map, [key, val]);
  const mapHas = (map, key) => $ReflectApply($MapHas, map, [key]);

  // Need to unwrap our own proxies when multiple extensions run this shim.
  const realLocalStorage = window.localStorage;
  const realSessionStorage = window.sessionStorage;
  let localStorageProxy;
  let sessionStorageProxy;
  function unwrapStorage(storage) {
    if (storage === localStorageProxy) {
      return realLocalStorage;
    }
    if (storage === sessionStorageProxy) {
      return realSessionStorage;
    }
    return storage;
  }

  const originalToStrings = new Map();

  const storageGetItemDesc = Object.getOwnPropertyDescriptor(
    Storage.prototype, "getItem"
  );
  const originalStorageGetItem = storageGetItemDesc.value;

  // =================== Conditional application of the shim ===================
  function shouldShimStorage() {
    const config = getConfig(window.sessionStorage) ||
      getConfig(window.localStorage);
    return Boolean(config);
  }

  if (!shouldShimStorage()) {
    return;
  }

  // ===================== Storage.prototype.getItem ======================
  // @docs https://developer.mozilla.org/en-US/docs/Web/API/Storage/getItem
  function getConfig(storage) {
    try {
      const configSerialized = $ReflectApply(
        originalStorageGetItem, unwrapStorage(storage),
        [CACHED_FILTERS_CONFIG_KEY]
      );
      if (configSerialized) {
        return $JSONparse(configSerialized);
      }
    }
    catch (e) {
      // If we can't parse, return null
    }
    return null;
  }

  function websiteHasValue(config) {
    return config && typeof config.websiteValue === "string";
  }
  const storageGetItemProxy = new Proxy(originalStorageGetItem, {
    apply(target, thisArg, argumentsList) {
      const key = argumentsList[0];
      const unwrappedThis = unwrapStorage(thisArg);
      if (key === CACHED_FILTERS_CONFIG_KEY) {
        const config = getConfig(unwrappedThis);
        if (websiteHasValue(config)) {
          return config.websiteValue;
        }
        return null;
      }
      return $ReflectApply(target, unwrappedThis, argumentsList);
    }
  });
  Object.defineProperty(Storage.prototype, "getItem", {
    ...storageGetItemDesc,
    value: storageGetItemProxy
  });
  mapSet(
    originalToStrings,
    storageGetItemProxy,
    originalStorageGetItem.toString.bind(originalStorageGetItem)
  );

  // ===================== Storage.prototype.setItem ===========================
  // @docs https://developer.mozilla.org/en-US/docs/Web/API/Storage/setItem
  const storageSetItemDesc = Object.getOwnPropertyDescriptor(
    Storage.prototype, "setItem"
  );
  const originalStorageSetItem = storageSetItemDesc.value;
  const storageSetItemProxy = new Proxy(originalStorageSetItem, {
    apply(target, thisArg, argumentsList) {
      const key = argumentsList[0];
      const unwrappedThis = unwrapStorage(thisArg);
      if (key === CACHED_FILTERS_CONFIG_KEY) {
        const config = getConfig(unwrappedThis) || {};
        config.websiteValue = $String(argumentsList[1]);
        $ReflectApply(
          target,
          unwrappedThis,
          [CACHED_FILTERS_CONFIG_KEY, $JSONstringify(config)]
        );
        return void 0;
      }
      return $ReflectApply(target, unwrappedThis, argumentsList);
    }
  });
  Object.defineProperty(Storage.prototype, "setItem", {
    ...storageSetItemDesc,
    value: storageSetItemProxy
  });
  mapSet(
    originalToStrings,
    storageSetItemProxy,
    originalStorageSetItem.toString.bind(originalStorageSetItem)
  );

  // ================== Storage.prototype.removeItem ==========================
  // @docs https://developer.mozilla.org/en-US/docs/Web/API/Storage/removeItem
  const storageRemoveItemDesc = Object.getOwnPropertyDescriptor(
    Storage.prototype, "removeItem"
  );
  const originalStorageRemoveItem = storageRemoveItemDesc.value;
  const storageRemoveItemProxy = new Proxy(originalStorageRemoveItem, {
    apply(target, thisArg, argumentsList) {
      const key = argumentsList[0];
      const unwrappedThis = unwrapStorage(thisArg);
      if (key === CACHED_FILTERS_CONFIG_KEY) {
        const config = getConfig(unwrappedThis);
        if (websiteHasValue(config)) {
          delete config.websiteValue;
          $ReflectApply(
            originalStorageSetItem,
            unwrappedThis, [CACHED_FILTERS_CONFIG_KEY, $JSONstringify(config)]
          );
        }
        return void 0;
      }
      return $ReflectApply(target, unwrappedThis, argumentsList);
    }
  });
  Object.defineProperty(Storage.prototype, "removeItem", {
    ...storageRemoveItemDesc,
    value: storageRemoveItemProxy
  });
  mapSet(
    originalToStrings,
    storageRemoveItemProxy,
    originalStorageRemoveItem.toString.bind(originalStorageRemoveItem)
  );

  // ==================== Storage.prototype.clear ============================
  // @docs https://developer.mozilla.org/en-US/docs/Web/API/Storage/clear
  const storageClearDesc = Object.getOwnPropertyDescriptor(
    Storage.prototype, "clear"
  );
  const originalStorageClear = storageClearDesc.value;
  const storageClearProxy = new Proxy(originalStorageClear, {
    apply(target, thisArg, argumentsList) {
      const unwrappedThis = unwrapStorage(thisArg);
      const config = getConfig(unwrappedThis);
      if (config) {
        delete config.websiteValue;
      }

      $ReflectApply(target, unwrappedThis, argumentsList);

      // Restore our config (without websiteValue)
      if (config && $ObjectKeys(config).length > 0) {
        $ReflectApply(
          originalStorageSetItem,
          unwrappedThis, [CACHED_FILTERS_CONFIG_KEY, $JSONstringify(config)]
        );
      }
      return void 0;
    }
  });
  Object.defineProperty(Storage.prototype, "clear", {
    ...storageClearDesc,
    value: storageClearProxy
  });
  mapSet(
    originalToStrings,
    storageClearProxy,
    originalStorageClear.toString.bind(originalStorageClear)
  );

  // ===================== Storage.prototype.key ===============================
  // @docs https://developer.mozilla.org/en-US/docs/Web/API/Storage/key
  const storageKeyDesc = Object.getOwnPropertyDescriptor(
    Storage.prototype, "key"
  );
  const originalStorageKey = storageKeyDesc.value;
  const storageKeyProxy = new Proxy(originalStorageKey, {
    apply(target, thisArg, argumentsList) {
      const unwrappedThis = unwrapStorage(thisArg);
      const config = getConfig(unwrappedThis);
      if (!config || websiteHasValue(config)) {
        return $ReflectApply(target, unwrappedThis, argumentsList);
      }

      const requestedIndex = argumentsList[0];
      for (let i = 0; i <= requestedIndex; i++) {
        const key = $ReflectApply(target, unwrappedThis, [i]);
        if (key === CACHED_FILTERS_CONFIG_KEY) {
          return $ReflectApply(target, unwrappedThis, [requestedIndex + 1]);
        }
      }
      return $ReflectApply(target, unwrappedThis, argumentsList);
    }
  });
  Object.defineProperty(Storage.prototype, "key", {
    ...storageKeyDesc,
    value: storageKeyProxy
  });
  mapSet(
    originalToStrings,
    storageKeyProxy,
    originalStorageKey.toString.bind(originalStorageKey)
  );

  // =================== Storage.prototype.length ============================
  // @docs https://developer.mozilla.org/en-US/docs/Web/API/Storage/length
  const storageLengthDesc = Object.getOwnPropertyDescriptor(
    Storage.prototype, "length"
  );
  const originalStorageLengthGetter = storageLengthDesc.get;
  Object.defineProperty(Storage.prototype, "length", {
    ...storageLengthDesc,
    get() {
      const unwrappedThis = unwrapStorage(this);
      const originalLength =
        $ReflectApply(originalStorageLengthGetter, unwrappedThis, []);
      const config = getConfig(unwrappedThis);
      if (config && !websiteHasValue(config)) {
        return originalLength - 1;
      }
      return originalLength;
    }
  });

  // ================== Proxy wrapper for localStorage ===========
  // Handles: {...localStorage}, Object.keys(), Object.values(), for...in, etc.
  const methodProxyCache = new Map();

  function getMethodProxy(storage, method) {
    if (mapHas(methodProxyCache, method)) {
      return mapGet(methodProxyCache, method);
    }
    const methodProxy = new Proxy(method, {
      apply(fn, _, args) {
        return $ReflectApply(fn, storage, args);
      }
    });
    mapSet(methodProxyCache, method, methodProxy);
    // Register toString for the wrapper to preserve function name
    const originalMethod = mapGet(originalToStrings, method);
    if (originalMethod) {
      mapSet(originalToStrings, methodProxy, originalMethod);
    }
    return methodProxy;
  }

  const storageInstanceProxyConfig = {
    ownKeys(target) {
      const keys = $ReflectOwnKeys(target);
      const config = getConfig(target);
      if (config && !websiteHasValue(config)) {
        return filter(keys, key => key !== CACHED_FILTERS_CONFIG_KEY);
      }
      return keys;
    },

    // Required for spread operator
    getOwnPropertyDescriptor(target, prop) {
      if (prop === CACHED_FILTERS_CONFIG_KEY) {
        const config = getConfig(target);
        if (config && !websiteHasValue(config)) {
          return void 0; // Hide the property entirely
        }
        // When website has set a value, return a proper enumerable descriptor
        // with the website's value (not our internal config)
        if (websiteHasValue(config)) {
          return {
            value: config.websiteValue,
            writable: true,
            enumerable: true,
            configurable: true
          };
        }
      }
      return $ReflectGetOwnPropertyDescriptor(target, prop);
    },

    // Needed for 'in' operator
    has(target, prop) {
      if (prop === CACHED_FILTERS_CONFIG_KEY) {
        const config = getConfig(target);
        if (config && !websiteHasValue(config)) {
          return false;
        }
      }
      return $ReflectHas(target, prop);
    },

    // Forward get/set using original target so native methods work correctly
    get(target, prop) {
      if (prop === CACHED_FILTERS_CONFIG_KEY) {
        return target.getItem(CACHED_FILTERS_CONFIG_KEY);
      }
      // Return correct toStringTag so Object.prototype.toString returns
      // [object Storage] instead of [object Object] (for older Firefox)
      if (prop === Symbol.toStringTag) {
        return "Storage";
      }
      const value = $ReflectGet(target, prop, target);
      // For methods, wrap in a proxy to bind `this` to original target
      // while preserving toString behavior
      if (typeof value === "function") {
        return getMethodProxy(target, value);
      }
      return value;
    },

    set(target, prop, value) {
      if (prop === CACHED_FILTERS_CONFIG_KEY) {
        target.setItem(CACHED_FILTERS_CONFIG_KEY, value);
        return true;
      }
      return $ReflectSet(target, prop, value, target);
    },

    defineProperty(target, prop, descriptor) {
      if (prop === CACHED_FILTERS_CONFIG_KEY) {
        if ("value" in descriptor) {
          target.setItem(CACHED_FILTERS_CONFIG_KEY, descriptor.value);
        }
        return true;
      }
      return $ReflectDefineProperty(target, prop, descriptor);
    },

    deleteProperty(target, prop) {
      if (prop === CACHED_FILTERS_CONFIG_KEY) {
        target.removeItem(CACHED_FILTERS_CONFIG_KEY);
        return true;
      }
      return $ReflectDeleteProperty(target, prop);
    }
  };

  localStorageProxy = new Proxy(
    window.localStorage,
    storageInstanceProxyConfig
  );

  Object.defineProperty(window, "localStorage", {
    value: localStorageProxy,
    writable: false,
    configurable: true,
    enumerable: true
  });

  sessionStorageProxy = new Proxy(
    window.sessionStorage,
    storageInstanceProxyConfig
  );

  Object.defineProperty(window, "sessionStorage", {
    value: sessionStorageProxy,
    writable: false,
    configurable: true,
    enumerable: true
  });

  // ===================== Function.prototype.toString =========================
  // @docs https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Function/toString
  const functionToStringDesc = Object.getOwnPropertyDescriptor(
    Function.prototype, "toString"
  );
  const originalFunctionToString = functionToStringDesc.value;
  const functionToStringProxy = new Proxy(originalFunctionToString, {
    apply(target, thisArg, argumentsList) {
      // Call "super" first, just in case the function was overwritten and had
      // checks if it was called
      const r = $ReflectApply(target, thisArg, argumentsList);

      const restoredToString = mapGet(originalToStrings, thisArg);
      if (restoredToString) {
        return $ReflectApply(restoredToString, thisArg, argumentsList);
      }

      return r;
    }
  });
  Object.defineProperty(Function.prototype, "toString", {
    ...functionToStringDesc,
    value: functionToStringProxy
  });
  mapSet(
    originalToStrings,
    functionToStringProxy,
    originalFunctionToString.toString.bind(originalFunctionToString)
  );
}

;// ./src/content/shared/helpers.js
/*
 * This file is part of eyeo's Web Extension Ad Blocking Toolkit (EWE),
 * Copyright (C) 2006-present eyeo GmbH
 *
 * EWE is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
 *
 * EWE is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with EWE.  If not, see <http://www.gnu.org/licenses/>.
 */



/**
 * Claims a communication channel name from the document's dataset.
 *
 * If a channel name already exists in the dataset, it is consumed (removed
 * from the dataset and returned). If no channel name exists, the fallback
 * channel is stored in the dataset and returned.
 *
 * This mechanism ensures that only one content script can claim the
 * channel name at a time, preventing conflicts when the main world
 * and isolated world scripts execution order is not consistent.
 * @see https://developer.mozilla.org/en-US/docs/Mozilla/Firefox/Releases/139#changes_for_add-on_developers
 * @see https://bugzil.la/1792685
 * @see https://eyeo.atlassian.net/wiki/spaces/B2C/pages/1666678786/Content-script+based+snippets
 *
 * @param {string} fallbackChannel - The channel name to use and store if
 *   none is present.
 * @returns {string} The claimed channel name (either the existing one
 *   or the fallback).
 */
function claimCommsChannel(fallbackChannel) {
  let channelName = document.documentElement.dataset[COMMS_CHANNEL_DATASET_KEY];

  if (!channelName) {
    channelName = fallbackChannel;
    document.documentElement.dataset[COMMS_CHANNEL_DATASET_KEY] = channelName;
  }
  else {
    delete document.documentElement.dataset[COMMS_CHANNEL_DATASET_KEY];
  }

  return channelName;
}

;// ./src/all/errors.js
/*
 * This file is part of eyeo's Web Extension Ad Blocking Toolkit (EWE),
 * Copyright (C) 2006-present eyeo GmbH
 *
 * EWE is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
 *
 * EWE is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with EWE.  If not, see <http://www.gnu.org/licenses/>.
 */

const ERROR_NO_CONNECTION = (/* unused pure expression or super */ null && ("Could not establish connection. " +
      "Receiving end does not exist."));
const ERROR_CLOSED_CONNECTION = (/* unused pure expression or super */ null && ("A listener indicated an asynchronous " +
      "response by returning true, but the message channel closed before a " +
      "response was received"));
// https://bugzilla.mozilla.org/show_bug.cgi?id=1578697
const ERROR_MANAGER_DISCONNECTED = "Message manager disconnected";

/**
 * Reconstructs an error from a serializable error object
 *
 * @param {Object} errorData - Error object
 *
 * @returns {Error} error
 */
function fromSerializableError(errorData) {
  const error = new Error(errorData.message);
  error.cause = errorData.cause;
  error.name = errorData.name;
  error.stack = errorData.stack;

  return error;
}

/**
 * Filters out `browser.runtime.sendMessage` errors to do with the receiving end
 * no longer existing.
 *
 * @param {Promise} promise The promise that should have "no connection" errors
 *   ignored. Generally this would be the promise returned by
 *   `browser.runtime.sendMessage`.
 * @return {Promise} The same promise, but will resolve with `undefined` instead
 *   of rejecting if the receiving end no longer exists.
 */
function ignoreNoConnectionError(promise) {
  return promise.catch(error => {
    if (typeof error == "object" &&
        (error.message == ERROR_NO_CONNECTION ||
         error.message == ERROR_CLOSED_CONNECTION ||
         error.message == ERROR_MANAGER_DISCONNECTED)) {
      return;
    }

    throw error;
  });
}

/**
 * Creates serializable error object from given error
 *
 * @param {Error} error - Error
 *
 * @returns {Object} serializable error object
 */
function toSerializableError(error) {
  return {
    cause: error.cause instanceof Error ?
      toSerializableError(error.cause) :
      error.cause,
    message: error.message,
    name: error.name,
    stack: error.stack
  };
}

;// ./src/content/main/snippets.entry.js
/*
 * This file is part of eyeo's Web Extension Ad Blocking Toolkit (EWE),
 * Copyright (C) 2006-present eyeo GmbH
 *
 * EWE is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
 *
 * EWE is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with EWE.  If not, see <http://www.gnu.org/licenses/>.
 */

/* global chrome browser */








// Use chrome.storage to detect if we're in an isolated world.
// Note: chrome.runtime is unreliable since other extensions may expose it
// in the main world.
const isMainWorld = !(
  (typeof chrome === "object" && !!chrome.storage) ||
  (typeof browser === "object" && !!browser.storage)
);

// Get or create a unique channel name for communicating with the isolated world
const commsChannelName = claimCommsChannel(esm_browser_v4());

const runStorageShim = (shimFn, configKey) => {
  try {
    if (typeof shimFn === "function" && configKey) {
      shimFn(configKey);
    }
  }
  catch (err) {
    // It would be good to report this error to Sentry, but we don't currently
    // have a way to do that from the main world.
  }
};

const runSnippets = snippetsConfig => {
  const {callback, filters, env, commsChannel, serializeError} = snippetsConfig;

  if (filters.length) {
    try {
      callback(env, ...filters);
    }
    catch (e) {
      // It would be good to report this error to Sentry, but we don't currently
      // have a way to do that from the main world.
      const errorEvent = new CustomEvent(commsChannel, {
        detail: {
          type: "ewe:main-error",
          error: serializeError(e)
        }
      });
      document.dispatchEvent(errorEvent);
    }
  }
};

const createTrustedScriptPolicy = () => {
  const isTrustedTypesSupported = typeof trustedTypes !== "undefined";
  let policy = null;

  try {
    if (isTrustedTypesSupported) {
      policy = trustedTypes.createPolicy(esm_browser_v4(), {
        createScript: code => code,
        createScriptURL: url => url
      });
    }
  }
  catch (_) {
  }
  return policy;
};

const injectScript = (executable, policy) => {
  const script = document.createElement("script");
  script.type = "application/javascript";
  script.async = false;

  if (policy) {
    script.textContent = policy.createScript(executable);
  }
  else {
    script.textContent = executable;
  }

  try {
    document.documentElement.appendChild(script);
  }
  catch (_) {}
  document.documentElement.removeChild(script);
};

const appendSnippets = snippetsConfig => {
  const policy = createTrustedScriptPolicy();
  const {
    callback,
    filters,
    env,
    shimFn,
    shimConfigKey,
    commsChannel,
    serializeError
  } = snippetsConfig;

  const snippetsCode = filters.length ? `
    const callback = (${callback});
    const runSnippets = (${runSnippets});
    const serializeError = (${serializeError});
    const snippetsConfig = {
      callback,
      env: ${JSON.stringify(env)},
      filters: ${JSON.stringify(filters)},
      commsChannel: "${commsChannel}",
      serializeError
    };
    runSnippets(snippetsConfig);
  ` : "";

  const code = `(function () {
    const shimFn = (${shimFn});
    const shimConfigKey = "${shimConfigKey}";
    const runStorageShim = (${runStorageShim});
    runStorageShim(shimFn, shimConfigKey);
    ${snippetsCode}
  })();`;

  injectScript(code, policy);
};

const onFiltersReceived = event => {
  if (!event || !event.detail) {
    return;
  }

  const {type, filters, debug} = event.detail;

  // ignore other events that are not related to filters config
  if (type !== "ewe:filters-config") {
    return;
  }

  // Check which snippets need to be executed in the main world.
  const mainSnippets = [];
  for (const filter of filters) {
    for (const [name, ...args] of filter) {
      if (main.has(name)) {
        mainSnippets.push([name, ...args]);
      }
    }
  }

  const snippetsConfig = {
    callback: main,
    env: {debugCSSProperties: debug ? DEBUG_CSS_PROPERTIES : null},
    filters: mainSnippets,
    shimFn: shimStorage,
    shimConfigKey: CACHED_FILTERS_CONFIG_KEY,
    commsChannel: commsChannelName,
    serializeError: toSerializableError
  };

  // If this script is injected into the main world we can execute directly.
  // If we are on isolated world (MV2), we need to create an inline script to
  // inject the snippets into page context.
  if (isMainWorld) {
    runStorageShim(shimStorage, CACHED_FILTERS_CONFIG_KEY);
    runSnippets(snippetsConfig);
  }
  else {
    appendSnippets(snippetsConfig);
  }
};

document.addEventListener(commsChannelName, onFiltersReceived);
document.dispatchEvent(new CustomEvent(HANDSHAKE_EVENT_NAME));

/******/ })()
;
