using _7VBPanel.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _7VBPanel.Utils
{
    public static class CS2Optimizer
    {
        private const string GameStateConfig = "IkNTR1NJIEV4YW1wbGUiDQp7DQoJInVyaSIgImh0dHA6Ly9sb2NhbGhvc3Q6MzAwMCINCgkidGltZW91dCIgIjEuMCINCgkiYXV0aCINCgl7DQoJCSJ0b2tlbiIJCQkJIkNTR1NJIFRlc3QiDQoJfQ0KCSJkYXRhIg0KCXsNCgkJInByb3ZpZGVyIiAgICAgICAgICAgICAgCSIxIg0KCQkibWFwIiAgICAgICAgICAgICAgICAgICAJIjEiDQoJCSJyb3VuZCIgICAgICAgICAgICAgICAgIAkiMSINCgkJInBsYXllcl9pZCIJCQkJCSIxIg0KCQkicGxheWVyX3dlYXBvbnMiCQkJIjEiDQoJCSJwbGF5ZXJfbWF0Y2hfc3RhdHMiCQkiMSINCgkJInBsYXllcl9zdGF0ZSIJCQkJIjEiDQoJCSJhbGxwbGF5ZXJzX2lkIgkJCQkiMSINCgkJImFsbHBsYXllcnNfc3RhdGUiCQkJIjEiDQoJCSJhbGxwbGF5ZXJzX21hdGNoX3N0YXRzIgkiMSINCgl9DQp9";

        private const string CS2ConvarsConfig = "ImNvbmZpZyIKewoJImNvbnZhcnMiCgl7CgkJImFkc3BfZGVidWciCQkiMCIKCQkiYmF0dGVyeV9zYXZlciIJCSJmYWxzZSIKCQkiY19tYXhkaXN0YW5jZSIJCSIyMDAuMDAwMDAwIgoJCSJjX21heHBpdGNoIgkJIjkwLjAwMDAwMCIKCQkiY19tYXh5YXciCQkiMTM1LjAwMDAwMCIKCQkiY19taW5kaXN0YW5jZSIJCSIzMC4wMDAwMDAiCgkJImNfbWlucGl0Y2giCQkiMC4wMDAwMDAiCgkJImNfbWlueWF3IgkJIi0xMzUuMDAwMDAwIgoJCSJjX29ydGhvaGVpZ2h0IgkJIjEwMC4wMDAwMDAiCgkJImNfb3J0aG93aWR0aCIJCSIxMDAuMDAwMDAwIgoJCSJjX3RoaXJkcGVyc29uc2hvdWxkZXIiCQkiZmFsc2UiCgkJImNfdGhpcmRwZXJzb25zaG91bGRlcmFpbWRpc3QiCQkiMTIwLjAwMDAwMCIKCQkiY190aGlyZHBlcnNvbnNob3VsZGVyZGlzdCIJCSI0MC4wMDAwMDAiCgkJImNfdGhpcmRwZXJzb25zaG91bGRlcmhlaWdodCIJCSI1LjAwMDAwMCIKCQkiY190aGlyZHBlcnNvbnNob3VsZGVyb2Zmc2V0IgkJIjIwLjAwMDAwMCIKCQkiY2FjaGVkdmFsdWVfY291bnRfcGFydHlicm93c2VyIgkJIjE2OTU4NTA0OTgiCgkJImNhY2hlZHZhbHVlX2NvdW50X3RlYW1tYXRlcyIJCSIwIgoJCSJjYW1fY29sbGlzaW9uIgkJIjEiCgkJImNhbV9pZGVhbGRlbHRhIgkJIjQuMDAwMDAwIgoJCSJjYW1faWRlYWxkaXN0IgkJIjE1MC4wMDAwMDAiCgkJImNhbV9pZGVhbGxhZyIJCSI0LjAwMDAwMCIKCQkiY2FtX2lkZWFscGl0Y2giCQkiMC4wMDAwMDAiCgkJImNhbV9pZGVhbHlhdyIJCSIwLjAwMDAwMCIKCQkiY2FtX3NuYXB0byIJCSJmYWxzZSIKCQkiY2NfZGVsYXlfdGltZSIJCSIwLjI1MDAwMCIKCQkiY2NfbGluZ2VyX3RpbWUiCQkiMS4wMDAwMDAiCgkJImNjX3NwZWN0YXRvcl9vbmx5IgkJImZhbHNlIgoJCSJjY19zdWJ0aXRsZXMiCQkiZmFsc2UiCgkJImNjX3ZyX2NhcHRpb25fc3BlZWQiCQkiMSIKCQkiY2NfdnJfZm9udF9zaXplIgkJIjEiCgkJImNjX3ZyX3dpZHRoIgkJIjEiCgkJImNsX2FsbG93X2FuaW1hdGVkX2F2YXRhcnMiCQkiZmFsc2UiCgkJImNsX2F1dG9fY3Vyc29yX3NjYWxlIgkJInRydWUiCgkJImNsX2F1dG9oZWxwIgkJImZhbHNlIgoJCSJjbF9jaGF0ZmlsdGVycyIJCSI2MyIKCQkiY2xfY2xhbmlkIgkJIjAiCgkJImNsX2NvbG9yIgkJIjEiCgkJImNsX2Nyb3NzaGFpcl9mcmllbmRseV93YXJuaW5nIgkJIjEiCgkJImNsX2N1cnNvcl9zY2FsZSIJCSIxLjAwMDAwMCIKCQkiY2xfZGVhdGhjYW1wYW5lbF9wb3NpdGlvbl9keW5hbWljIgkJIjEiCgkJImNsX2Rpc2FibGVfcm91bmRfZW5kX3JlcG9ydCIJCSJmYWxzZSIKCQkiY2xfZG1fYnV5cmFuZG9td2VhcG9ucyIJCSJ0cnVlIgoJCSJjbF9lbWJlZGRlZF9zdHJlYW1fYXVkaW9fdm9sdW1lIgkJIjYwLjAwMDAwMCIKCQkiY2xfZW1iZWRkZWRfc3RyZWFtX2F1ZGlvX3ZvbHVtZV94bWFzdGVyIgkJInRydWUiCgkJImNsX2VudF9waXZvdF9zaXplIgkJIjIwLjAwMDAwMCIKCQkiY2xfZW50X3RleHRfZmxhZ3NfYWN0aXZlIgkJIi0xIgoJCSJjbF9oaWRlX2F2YXRhcl9pbWFnZXMiCQkiMCIKCQkiY2xfaHVkX2NvbG9yIgkJIjQiCgkJImNsX2h1ZF9yYWRhcl9zY2FsZSIJCSIxLjAwMDAwMCIKCQkiY2xfaW1wb3J0X2NzZ29fY29uZmlnIgkJImZhbHNlIgoJCSJjbF9pbnRlcnBvbGF0ZV9yZXBvcnQiCQkiZmFsc2UiCgkJImNsX2ludmVudG9yeV9zYXZlZF9maWx0ZXIyIgkJImFsbCIKCQkiY2xfaW52ZW50b3J5X3NhdmVkX3NvcnQyIgkJImludl9zb3J0X2FnZSIKCQkiY2xfaW52aXRlc19vbmx5X2ZyaWVuZHMiCQkiZmFsc2UiCgkJImNsX2ludml0ZXNfb25seV9tYWlubWVudSIJCSJmYWxzZSIKCQkiY2xfaXRlbWltYWdlc19keW5hbWljYWxseV9nZW5lcmF0ZWQiCQkiMiIKCQkiY2xfam9pbl9hZHZlcnRpc2UiCQkiMSIKCQkiY2xfbGF0Y2hfcmVwb3J0IgkJImZhbHNlIgoJCSJjbF9sb2Fkb3V0X3NhdmVkX3NvcnQiCQkiaW52X3NvcnRfYWdlIgoJCSJjbF9tdXRlX2FsbF9idXRfZnJpZW5kc19hbmRfcGFydHkiCQkiMCIKCQkiY2xfbXV0ZV9lbmVteV90ZWFtIgkJImZhbHNlIgoJCSJjbF9uZXdfdXNlcl9waGFzZSIJCSItMSIKCQkiY2xfb2JzX2ludGVycF9lbmFibGUiCQkidHJ1ZSIKCQkiY2xfb2JzX2ludGVycF9wb3NfcmF0ZSIJCSIwLjI3MDAwMCIKCQkiY2xfb2JzZXJ2ZWRfYm90X2Nyb3NzaGFpciIJCSIyIgoJCSJjbF9waW5nX2ZhZGVfZGVhZHpvbmUiCQkiNjAuMDAwMDAwIgoJCSJjbF9waW5nX2ZhZGVfZGlzdGFuY2UiCQkiMzAwLjAwMDAwMCIKCQkiY2xfcGxheWVyX3BpbmdfbXV0ZSIJCSIwIgoJCSJjbF9wbGF5ZXJzcHJheV9hdXRvX2FwcGx5IgkJInRydWUiCgkJImNsX3BsYXllcnNwcmF5ZGlzYWJsZSIJCSJmYWxzZSIKCQkiY2xfcHJvbW90ZWRfc2V0dGluZ3NfYWNrbm93bGVkZ2VkIgkJIjE6MTY5NTg0OTYxMzI5OSIKCQkiY2xfcXVpY2tpbnZlbnRvcnlfZmlsZW5hbWUiCQkicmFkaWFsX3F1aWNraW52ZW50b3J5LnR4dCIKCQkiY2xfcXVpY2tpbnZlbnRvcnlfbGFzdGludiIJCSJ0cnVlIgoJCSJjbF9xdWlja2ludmVudG9yeV9saW5lX3VwZGF0ZV9zcGVlZCIJCSI2NS4wMDAwMDAiCgkJImNsX3JhZGFyX2Fsd2F5c19jZW50ZXJlZCIJCSJ0cnVlIgoJCSJjbF9yYWRhcl9pY29uX3NjYWxlX21pbiIJCSIwLjYwMDAwMCIKCQkiY2xfcmFkYXJfcm90YXRlIgkJInRydWUiCgkJImNsX3JhZGFyX3NjYWxlIgkJIjAuNzAwMDAwIgoJCSJjbF9yYWRhcl9zcXVhcmVfd2l0aF9zY29yZWJvYXJkIgkJInRydWUiCgkJImNsX3JhZGlhbF9yYWRpb190YWJfMF90ZXh0XzEiCQkiI0NoYXR3aGVlbF9yZXF1ZXN0c3BlbmQiCgkJImNsX3JhZGlhbF9yYWRpb190YWJfMF90ZXh0XzIiCQkiI0NoYXR3aGVlbF9yZXF1ZXN0d2VhcG9uIgoJCSJjbF9yYWRpYWxfcmFkaW9fdGFiXzBfdGV4dF8zIgkJIiNDaGF0d2hlZWxfYnBsYW4iCgkJImNsX3JhZGlhbF9yYWRpb190YWJfMF90ZXh0XzQiCQkiI0NoYXR3aGVlbF9mb2xsb3dpbmd5b3UiCgkJImNsX3JhZGlhbF9yYWRpb190YWJfMF90ZXh0XzUiCQkiI0NoYXR3aGVlbF9taWRwbGFuIgoJCSJjbF9yYWRpYWxfcmFkaW9fdGFiXzBfdGV4dF82IgkJIiNDaGF0d2hlZWxfZm9sbG93bWUiCgkJImNsX3JhZGlhbF9yYWRpb190YWJfMF90ZXh0XzciCQkiI0NoYXR3aGVlbF9hcGxhbiIKCQkiY2xfcmFkaWFsX3JhZGlvX3RhYl8wX3RleHRfOCIJCSIjQ2hhdHdoZWVsX3JlcXVlc3RlY29yb3VuZCIKCQkiY2xfcmFkaWFsX3JhZGlvX3RhYl8xX3RleHRfMSIJCSIjQ2hhdHdoZWVsX2VuZW15c3BvdHRlZCIKCQkiY2xfcmFkaWFsX3JhZGlvX3RhYl8xX3RleHRfMiIJCSIjQ2hhdHdoZWVsX25lZWRiYWNrdXAiCgkJImNsX3JhZGlhbF9yYWRpb190YWJfMV90ZXh0XzMiCQkiI0NoYXR3aGVlbF9icGxhbiIKCQkiY2xfcmFkaWFsX3JhZGlvX3RhYl8xX3RleHRfNCIJCSIjQ2hhdHdoZWVsX2JvbWJjYXJyaWVyc3BvdHRlZCIKCQkiY2xfcmFkaWFsX3JhZGlvX3RhYl8xX3RleHRfNSIJCSIjQ2hhdHdoZWVsX211bHRpcGxlZW5lbWllc2hlcmUiCgkJImNsX3JhZGlhbF9yYWRpb190YWJfMV90ZXh0XzYiCQkiI0NoYXR3aGVlbF9zbmlwZXJzcG90dGVkIgoJCSJjbF9yYWRpYWxfcmFkaW9fdGFiXzFfdGV4dF83IgkJIiNDaGF0d2hlZWxfYXBsYW4iCgkJImNsX3JhZGlhbF9yYWRpb190YWJfMV90ZXh0XzgiCQkiI0NoYXR3aGVlbF9pbnBvc2l0aW9uIgoJCSJjbF9yYWRpYWxfcmFkaW9fdGFiXzJfdGV4dF8xIgkJIiNDaGF0d2hlZWxfYWZmaXJtYXRpdmUiCgkJImNsX3JhZGlhbF9yYWRpb190YWJfMl90ZXh0XzIiCQkiI0NoYXR3aGVlbF9uZWdhdGl2ZSIKCQkiY2xfcmFkaWFsX3JhZGlvX3RhYl8yX3RleHRfMyIJCSIjQ2hhdHdoZWVsX2NvbXBsaW1lbnQiCgkJImNsX3JhZGlhbF9yYWRpb190YWJfMl90ZXh0XzQiCQkiI0NoYXR3aGVlbF90aGFua3MiCgkJImNsX3JhZGlhbF9yYWRpb190YWJfMl90ZXh0XzUiCQkiI0NoYXR3aGVlbF9jaGVlciIKCQkiY2xfcmFkaWFsX3JhZGlvX3RhYl8yX3RleHRfNiIJCSIjQ2hhdHdoZWVsX3BlcHRhbGsiCgkJImNsX3JhZGlhbF9yYWRpb190YWJfMl90ZXh0XzciCQkiI0NoYXR3aGVlbF9zb3JyeSIKCQkiY2xfcmFkaWFsX3JhZGlvX3RhYl8yX3RleHRfOCIJCSIjQ2hhdHdoZWVsX3NlY3RvcmNsZWFyIgoJCSJjbF9yYWRpYWxfcmFkaW9fdGFwX3RvX3BpbmciCQkidHJ1ZSIKCQkiY2xfcmFkaWFsX3JhZGlvX3ZlcnNpb25fcmVzZXQiCQkiMTIiCgkJImNsX3JhZGlhbG1lbnVfZGVhZHpvbmVfc2l6ZV9qb3lzdGljayIJCSIwLjE3MDAwMCIKCQkiY2xfcmFnZG9sbF9saW1pdCIJCSIyMCIKCQkiY2xfcmVkZW1wdGlvbl9yZXNldF90aW1lc3RhbXAiCQkiMCIKCQkiY2xfc2FuaXRpemVfcGxheWVyX25hbWVzIgkJImZhbHNlIgoJCSJjbF9zY29yZWJvYXJkX21vdXNlX2VuYWJsZV9iaW5kaW5nIgkJIithdHRhY2syIgoJCSJjbF9zY29yZWJvYXJkX3N1cnZpdm9yc19hbHdheXNfb24iCQkiZmFsc2UiCgkJImNsX3Nob3dfY2xhbl9pbl9kZWF0aF9ub3RpY2UiCQkidHJ1ZSIKCQkiY2xfc2hvd19lcXVpcHBlZF9jaGFyYWN0ZXJfZm9yX3BsYXllcl9hdmF0YXJzIgkJImZhbHNlIgoJCSJjbF9zaG93X29ic2VydmVyX2Nyb3NzaGFpciIJCSIyIgoJCSJjbF9zbmlwZXJfZGVsYXlfdW5zY29wZSIJCSJmYWxzZSIKCQkiY2xfdGVhbWlkX292ZXJoZWFkX2NvbG9yc19zaG93IgkJImZhbHNlIgoJCSJjbF90ZWFtaWRfb3ZlcmhlYWRfbW9kZSIJCSIyIgoJCSJjbF90ZWFtbWF0ZV9jb2xvcnNfc2hvdyIJCSIxIgoJCSJjbF90aW1lb3V0IgkJIjMwLjAwMDAwMCIKCQkiY2xfdmVyc3VzX2ludHJvIgkJInRydWUiCgkJImNsb3NlY2FwdGlvbiIJCSJmYWxzZSIKCQkiY29tbWVudGFyeSIJCSJmYWxzZSIKCQkiY29uX2VuYWJsZSIJCSJ0cnVlIgoJCSJjc2dvX21hcF9wcmV2aWV3X3NjYWxlIgkJIjIuMDAwMDAwIgoJCSJkZW1vX2ZsdXNoIgkJImZhbHNlIgoJCSJkc3Bfdm9sdW1lIgkJIjAuODAwMDAwIgoJCSJlbmFibGVfYm9uZWZsZXgiCQkidHJ1ZSIKCQkiZW5naW5lX25vX2ZvY3VzX3NsZWVwIgkJIjIwIgoJCSJlbnRfcGl2b3Rfc2l6ZSIJCSIyMC4wMDAwMDAiCgkJImVudF90ZXh0X2ZsYWdzX2FjdGl2ZSIJCSItMSIKCQkiZm92X2Rlc2lyZWQiCQkiNzUuMDAwMDAwIgoJCSJmcHNfbWF4IgkJIjQwLjAwMDAwMCIKCQkiZnBzX21heF90b29scyIJCSIzMC4wMDAwMDAiCgkJImZwc19tYXhfdWkiCQkiMzAuMDAwMDAwIgoJCSJmdW5jX2JyZWFrX21heF9waWVjZXMiCQkiMTUiCgkJImh1ZF9mYXN0c3dpdGNoIgkJIjAiCgkJImh1ZF9zY2FsaW5nJDMiCQkiMC45OTY0OTkiCgkJImlucHV0X2J1dHRvbl9jb2RlX2lzX3NjYW5fY29kZSIJCSJ0cnVlIgoJCSJpbnB1dF9maWx0ZXJfcmVsYXRpdmVfYW5hbG9nX2lucHV0cyIJCSJmYWxzZSIKCQkiam95X2FkdmFuY2VkIgkJImZhbHNlIgoJCSJqb3lfYWR2YXhpc3IiCQkiMCIKCQkiam95X2FkdmF4aXN1IgkJIjAiCgkJImpveV9hZHZheGlzdiIJCSIwIgoJCSJqb3lfYWR2YXhpc3giCQkiMCIKCQkiam95X2FkdmF4aXN5IgkJIjAiCgkJImpveV9hZHZheGlzeiIJCSIwIgoJCSJqb3lfYXhpc2J1dHRvbl90aHJlc2hvbGQiCQkiMC4zMDAwMDAiCgkJImpveV9kaXNwbGF5X2lucHV0IgkJImZhbHNlIgoJCSJqb3lfbmFtZSIJCSJqb3lzdGljayIKCQkiam95X3NpZGVzZW5zaXRpdml0eSIJCSIxLjAwMDAwMCIKCQkiam95X3dpbmdtYW53YXJyaW9yX2NlbnRlcmhhY2siCQkiZmFsc2UiCgkJImpveV93aW5nbWFud2Fycmlvcl90dXJuaGFjayIJCSJmYWxzZSIKCQkiam95c3RpY2siCQkiZmFsc2UiCgkJImtleV9iaW5kX3ZlcnNpb24iCQkiNSIKCQkibG9iYnlfZGVmYXVsdF9wcml2YWN5X2JpdHMyIgkJIjEiCgkJImxvY2tNb3ZlQ29udHJvbGxlclJldCIJCSJmYWxzZSIKCQkibWFwb3ZlcnZpZXdfaWNvbl9zY2FsZSIJCSIxLjAwMDAwMCIKCQkibW1fY3Nnb19jb21tdW5pdHlfc2VhcmNoX3BsYXllcnNfbWluIgkJIjMiCgkJIm1tX2RlZGljYXRlZF9zZWFyY2hfbWF4cGluZyIJCSIzNTAiCgkJIm1tX3NlcnZlcl9zZWFyY2hfbGFuX3BvcnRzIgkJIjI3MDE1LDI3MDE2LDI3MDE3LDI3MDE4LDI3MDE5LDI3MDIwIgoJCSJtb2JpbGVfZnBzX2luY3JlYXNlX2R1cmluZ19jaGFyZ2luZyIJCSJmYWxzZSIKCQkibW9iaWxlX2Zwc19pbmNyZWFzZV9kdXJpbmdfdG91Y2giCQkidHJ1ZSIKCQkibW9iaWxlX2Zwc19saW1pdCIJCSIzMC4wMDAwMDAiCgkJIm1vdXNlX2ludmVydHkiCQkiZmFsc2UiCgkJIm5ldF9hbGxvd19tdWx0aWNhc3QiCQkidHJ1ZSIKCQkibmV0X21heHJvdXRhYmxlIgkJIjEyMDAiCgkJInBhbm9yYW1hX2NvbnNvbGVfcG9zaXRpb25fYW5kX3NpemUiCQkiMjAuMDB8MjAuMDB8MTAwMC4wMHw4MDAuMDAiCgkJInBhbm9yYW1hX2RlYnVnX292ZXJsYXlfb3BhY2l0eSIJCSIwLjI1MDAwMCIKCQkicGFub3JhbWFfZGVidWdfb3ZlcmxheV9vcGFjaXR5X21heCIJCSIwLjI1MDAwMCIKCQkicGFub3JhbWFfZGVidWdfb3ZlcmxheV9vcGFjaXR5X21pbiIJCSIwLjAxMDAwMCIKCQkicGFub3JhbWFfZGVidWdnZXJfdGhlbWUiCQkiTGlnaHQiCgkJInBhbm9yYW1hX2ZvY3VzX3dvcmxkX3BhbmVscyIJCSJmYWxzZSIKCQkicGxheWVyMF91c2luZ19qb3lzdGljayIJCSJmYWxzZSIKCQkicGxheWVyX2JvdGRpZmZsYXN0X3MiCQkiMiIKCQkicGxheWVyX2NvbXBldGl0aXZlX21hcGxpc3RfMnYyXzEwXzBfQzhEODg5ODYiCQkibWdfZGVfdmVydGlnbyIKCQkicGxheWVyX2NvbXBldGl0aXZlX21hcGxpc3RfOF8xMF8wXzUwNjk3NjkiCQkibWdfZGVfdmVydGlnbyIKCQkicGxheWVyX3N1cnZpdmFsX2xpc3RfMTBfMF8zMDMiCQkibWdfZHpfYmxhY2tzaXRlLG1nX2R6X3Npcm9jY28sbWdfZHpfdmluZXlhcmQsbWdfZHpfZW1iZXIiCgkJInJfZnVsbHNjcmVlbl9nYW1tYSIJCSIyLjIwMDAwMCIKCQkicl9wbGF5ZXJfdmlzaWJpbGl0eV9tb2RlIgkJIjAiCgkJInJfc2hvd19idWlsZF9pbmZvIgkJInRydWUiCgkJInJhdGUiCQkiNzg2NDMyIgoJCSJycl90aGVuYW55X3Njb3JlX3Nsb3AiCQkiMC4wMDAwMDAiCgkJInNhZmV6b25leCIJCSIxLjAwMDAwMCIKCQkic2FmZXpvbmV5IgkJIjEuMDAwMDAwIgoJCSJza19hdXRvYWltX21vZGUiCQkiMSIKCQkic25kX2F1dG9kZXRlY3RfbGF0ZW5jeSIJCSJ0cnVlIgoJCSJzbmRfZGVhdGhjYW1lcmFfdm9sdW1lJDQiCQkiMC4xNjAwMDAiCgkJInNuZF9kdWNrZXJhdHRhY2t0aW1lIgkJIjAuNTAwMDAwIgoJCSJzbmRfZHVja2VycmVsZWFzZXRpbWUiCQkiMi41MDAwMDAiCgkJInNuZF9kdWNrZXJ0aHJlc2hvbGQiCQkiMC4xNTAwMDAiCgkJInNuZF9kdWNrdG92b2x1bWUiCQkiMC41NTAwMDAiCgkJInNuZF9nYWluIgkJIjEuMDAwMDAwIgoJCSJzbmRfZ2FtZXZvaWNldm9sdW1lIgkJIjEuMDAwMDAwIgoJCSJzbmRfZ2FtZXZvbHVtZSIJCSIxLjAwMDAwMCIKCQkic25kX2hlYWRwaG9uZV9lcSIJCSIwIgoJCSJzbmRfbWFwb2JqZWN0aXZlX3ZvbHVtZSQ0IgkJIjAuMDQwMDAwIgoJCSJzbmRfbWVudW11c2ljX3ZvbHVtZSQ0IgkJIjAuMDQwMDAwIgoJCSJzbmRfbWl4YWhlYWQiCQkiMC4wMDEwMDAiCgkJInNuZF9tdXNpY19zZWxlY3Rpb24iCQkiMSIKCQkic25kX211c2ljdm9sdW1lJDIiCQkiMS4wMDAwMDAiCgkJInNuZF9tdXRlX2xvc2Vmb2N1cyIJCSJ0cnVlIgoJCSJzbmRfbXV0ZV9tdnBfbXVzaWNfbGl2ZV9wbGF5ZXJzIgkJImZhbHNlIgoJCSJzbmRfbXZwX3ZvbHVtZSQ0IgkJIjAuMTYwMDAwIgoJCSJzbmRfcm91bmRhY3Rpb25fdm9sdW1lJDQiCQkiMC4wMDAwMDAiCgkJInNuZF9yb3VuZGVuZF92b2x1bWUkNCIJCSIwLjE2MDAwMCIKCQkic25kX3JvdW5kc3RhcnRfdm9sdW1lJDQiCQkiMC4wMDAwMDAiCgkJInNuZF9zcGF0aWFsaXplX2xlcnAiCQkiMC4wMDAwMDAiCgkJInNuZF9zdGVhbWF1ZGlvX2VuYWJsZV9wZXJzcGVjdGl2ZV9jb3JyZWN0aW9uIgkJImZhbHNlIgoJCSJzbmRfc3RlYW1hdWRpb19zb3VyY2VfcGF0aGluZ19kZWJ1ZyIJCSJmYWxzZSIKCQkic25kX3RlbnNlY29uZHdhcm5pbmdfdm9sdW1lJDQiCQkiMC4wNDAwMDAiCgkJInNuZF90b29sdm9sdW1lIgkJIjEuMDAwMDAwIgoJCSJzbmRfdm9pcHZvbHVtZSIJCSIxLjAwMDAwMCIKCQkic3BlYWtlcl9jb25maWciCQkiLTEiCgkJInNwZWNfY2VudGVyY2hhc2VjYW0iCQkiZmFsc2UiCgkJInNwZWNfcmVwbGF5X2F1dG9zdGFydCIJCSJ0cnVlIgoJCSJzcGVjX3Nob3dfeHJheSIJCSIxIgoJCSJzcGVjX3VzZW51bWJlcmtleXNfbm9iaW5kcyIJCSJ0cnVlIgoJCSJzcGxpdHNjcmVlbl9tb2RlIgkJIjAiCgkJInN2X2xvZ19vbmVmaWxlIgkJImZhbHNlIgoJCSJzdl9sb2diYW5zIgkJImZhbHNlIgoJCSJzdl9sb2dlY2hvIgkJInRydWUiCgkJInN2X2xvZ2ZpbGUiCQkiZmFsc2UiCgkJInN2X2xvZ2ZsdXNoIgkJImZhbHNlIgoJCSJzdl9sb2dzZGlyIgkJImxvZ3MiCgkJInN2X25vY2xpcGFjY2VsZXJhdGUiCQkiNS4wMDAwMDAiCgkJInN2X25vY2xpcGZyaWN0aW9uIgkJIjQuMDAwMDAwIgoJCSJzdl9ub2NsaXBzcGVlZCIJCSIxMjAwLjAwMDAwMCIKCQkic3ZfcGF1c2Vfb25fY29uc29sZV9vcGVuIgkJImZhbHNlIgoJCSJzdl9za3luYW1lIgkJInNreV91cmIwMSIKCQkic3Zfc3BlY2FjY2VsZXJhdGUiCQkiNS4wMDAwMDAiCgkJInN2X3NwZWNub2NsaXAiCQkidHJ1ZSIKCQkic3Zfc3BlY3NwZWVkJDIiCQkiMTIwMC4wMDAwMDAiCgkJInN2X3VubG9ja2VkY2hhcHRlcnMiCQkiMSIKCQkic3Zfdm9pY2VlbmFibGUiCQkidHJ1ZSIKCQkidHJ1c3RlZF9sYXVuY2giCQkiMCIKCQkidHZfbm9jaGF0IgkJImZhbHNlIgoJCSJ1aV9kZWVwc3RhdHNfcmFkaW9faGVhdF9maWd1cmluZSIJCSIwIgoJCSJ1aV9kZWVwc3RhdHNfcmFkaW9faGVhdF90YWIiCQkiMCIKCQkidWlfZGVlcHN0YXRzX3JhZGlvX2hlYXRfdGVhbSIJCSIwIgoJCSJ1aV9kZWVwc3RhdHNfdG9wbGV2ZWxfbW9kZSIJCSIwIgoJCSJ1aV9tYWlubWVudV9ia2duZF9tb3ZpZV9CMEI3MENGRiQxMCIJCSJkZV92ZXJ0aWdvIgoJCSJ1aV9uZWFyYnlsb2JiaWVzX2ZpbHRlcjMiCQkiY29tcGV0aXRpdmUiCgkJInVpX3BsYXlzZXR0aW5nc19mbGFnc19saXN0ZW5fY2FzdWFsIgkJIjAiCgkJInVpX3BsYXlzZXR0aW5nc19mbGFnc19saXN0ZW5fY29tcGV0aXRpdmUiCQkiMTYiCgkJInVpX3BsYXlzZXR0aW5nc19mbGFnc19saXN0ZW5fY29vcGVyYXRpdmUiCQkiMCIKCQkidWlfcGxheXNldHRpbmdzX2ZsYWdzX2xpc3Rlbl9kZWF0aG1hdGNoIgkJIjMyIgoJCSJ1aV9wbGF5c2V0dGluZ3NfZmxhZ3NfbGlzdGVuX3NjcmltY29tcDJ2MiIJCSIwIgoJCSJ1aV9wbGF5c2V0dGluZ3NfZmxhZ3NfbGlzdGVuX3NraXJtaXNoIgkJIjAiCgkJInVpX3BsYXlzZXR0aW5nc19mbGFnc19saXN0ZW5fc3Vydml2YWwiCQkiMCIKCQkidWlfcGxheXNldHRpbmdzX2ZsYWdzX29mZmljaWFsX2Nhc3VhbCIJCSIwIgoJCSJ1aV9wbGF5c2V0dGluZ3NfZmxhZ3Nfb2ZmaWNpYWxfY29tcGV0aXRpdmUiCQkiMTYiCgkJInVpX3BsYXlzZXR0aW5nc19mbGFnc19vZmZpY2lhbF9jb29wZXJhdGl2ZSIJCSIwIgoJCSJ1aV9wbGF5c2V0dGluZ3NfZmxhZ3Nfb2ZmaWNpYWxfZGVhdGhtYXRjaCIJCSIzMiIKCQkidWlfcGxheXNldHRpbmdzX2ZsYWdzX29mZmljaWFsX3NjcmltY29tcDJ2MiIJCSIwIgoJCSJ1aV9wbGF5c2V0dGluZ3NfZmxhZ3Nfb2ZmaWNpYWxfc2tpcm1pc2giCQkiMCIKCQkidWlfcGxheXNldHRpbmdzX2ZsYWdzX29mZmljaWFsX3N1cnZpdmFsIgkJIjAiCgkJInVpX3BsYXlzZXR0aW5nc19tYXBzX2xpc3Rlbl9jYXN1YWwiCQkibWdfZGVfZHVzdDIiCgkJInVpX3BsYXlzZXR0aW5nc19tYXBzX2xpc3Rlbl9jb21wZXRpdGl2ZSIJCSJtZ19kZV92ZXJ0aWdvIgoJCSJ1aV9wbGF5c2V0dGluZ3NfbWFwc19saXN0ZW5fZGVhdGhtYXRjaCIJCSJtZ19kZV9kdXN0MiIKCQkidWlfcGxheXNldHRpbmdzX21hcHNfbGlzdGVuX3NjcmltY29tcDJ2MiIJCSJtZ19kZV9pbmZlcm5vIgoJCSJ1aV9wbGF5c2V0dGluZ3NfbWFwc19saXN0ZW5fc2tpcm1pc2giCQkibWdfc2tpcm1pc2hfYXJtc3JhY2UiCgkJInVpX3BsYXlzZXR0aW5nc19tYXBzX29mZmljaWFsX2Nhc3VhbCIJCSJtZ19kdXN0MjQ3IgoJCSJ1aV9wbGF5c2V0dGluZ3NfbWFwc19vZmZpY2lhbF9kZWF0aG1hdGNoIgkJIm1nX2R1c3QyNDciCgkJInVpX3BsYXlzZXR0aW5nc19tb2RlX2xpc3RlbiIJCSJjb21wZXRpdGl2ZSIKCQkidWlfcGxheXNldHRpbmdzX21vZGVfb2ZmaWNpYWxfdjIwIgkJImRlYXRobWF0Y2giCgkJInVpX3BsYXlzZXR0aW5nc19zdXJ2aXZhbF9zb2xvIgkJIjAiCgkJInVpX3BsYXlzZXR0aW5nc193YXJtdXBfbWFwX25hbWUiCQkiZGVfbWlyYWdlIgoJCSJ1aV9wb3B1cF93ZWFwb251cGRhdGVfdmVyc2lvbiIJCSIyNDAyIgoJCSJ1aV9zZXR0aW5nX2FkdmVydGlzZWZvcmhpcmVfYXV0byIJCSIxIgoJCSJ1aV9zZXR0aW5nX2FkdmVydGlzZWZvcmhpcmVfYXV0b19sYXN0IgkJIi9jb21wZXRpdGl2ZSIKCQkidWlfc2hvd19zdWJzY3JpcHRpb25fYWxlcnQiCQkiMCIKCQkidWlfc3RlYW1fb3ZlcmxheV9ub3RpZmljYXRpb25fcG9zaXRpb24iCQkidG9wcmlnaHQiCgkJInVpX3N0ZWFtX292ZXJsYXlfbm90aWZpY2F0aW9uX3Bvc2l0aW9uX2hvcnoiCQkiMTAwIgoJCSJ1aV9zdGVhbV9vdmVybGF5X25vdGlmaWNhdGlvbl9wb3NpdGlvbl92ZXJ0IgkJIjEwMCIKCQkidWlfdmFuaXR5c2V0dGluZ19sb2Fkb3V0c2xvdF9jdCIJCSJyaWZsZTQiCgkJInVpX3Zhbml0eXNldHRpbmdfdGVhbSIJCSJjdCIKCQkidmlld21vZGVsX2ZvdiIJCSI2MC4wMDAwMDAiCgkJInZpZXdtb2RlbF9vZmZzZXRfeCIJCSIxLjAwMDAwMCIKCQkidmlld21vZGVsX29mZnNldF95IgkJIjEuMDAwMDAwIgoJCSJ2aWV3bW9kZWxfb2Zmc2V0X3oiCQkiLTEuMDAwMDAwIgoJCSJ2aWV3bW9kZWxfcHJlc2V0cG9zIgkJIjEiCgkJInZpb2xlbmNlX2FibG9vZCIJCSJ0cnVlIgoJCSJ2aW9sZW5jZV9hZ2licyIJCSJ0cnVlIgoJCSJ2aW9sZW5jZV9oYmxvb2QiCQkidHJ1ZSIKCQkidmlvbGVuY2VfaGdpYnMiCQkidHJ1ZSIKCQkidm9pY2VfYWx3YXlzX3NhbXBsZV9taWMiCQkiZmFsc2UiCgkJInZvaWNlX21vZGVuYWJsZSIJCSJ0cnVlIgoJCSJ2b2ljZV90aHJlc2hvbGQiCQkiNDAwMC4wMDAwMDAiCgkJInZvbHVtZSIJCSIwLjAwMDAwMCIKCQkibWFwX2VuYWJsZV9iYWNrZ3JvdW5kX21hcHMiCQkiMCIKCQkiY2xfZ3JhcGhpY3NfZHJpdmVyX3dhcm5pbmdfZG9udF9zaG93X2FnYWluIgkJInRydWUiCgkJInVpX25ld3NfbGFzdF9yZWFkX2xpbmsiCQkiaHR0cHM6Ly93d3cuY291bnRlci1zdHJpa2UubmV0L25ld3NlbnRyeS80MDY0MDA0MjY0NjA1MjQ1NDU4IgoJfQp9Cg==";

        private const string LocalConfig = "IlVzZXJMb2NhbENvbmZpZ1N0b3JlIg0Kew0KCSJCcm9hZGNhc3QiDQoJew0KCQkiUGVybWlzc2lvbnMiCQkiMSINCgl9DQoJIlNvZnR3YXJlIg0KCXsNCgkJIlZhbHZlIg0KCQl7DQoJCQkic3RlYW0iDQoJCQl7DQoJCQkJImFwcHMiDQoJCQkJew0KCQkJCQkiNyINCgkJCQkJew0KCQkJCQkJImNsb3VkIg0KCQkJCQkJew0KCQkJCQkJCSJsYXN0X3N5bmNfc3RhdGUiCQkic3luY2hyb25pemVkIg0KCQkJCQkJfQ0KCQkJCQl9DQoJCQkJCSI3MzAiDQoJCQkJCXsNCgkJCQkJCSJMYXVuY2hPcHRpb25zIgkJIj8/Pz8iDQoJCQkJCX0NCgkJCQl9DQoJCQl9DQoJCX0NCgl9DQp9";


        public static void ConfigureAllFiles(string steam_id_32, string steam_path, string cs2_path, string login)
        {
            if (Directory.Exists(cs2_path))
            {
                Task.Run(async delegate
                {
                    await SetupCS2Files(steam_id_32, steam_path, cs2_path, login);
                }).Wait();
            }
        }
        public static async Task SetupCS2Files(string steam_id_32, string steam_path, string cs2_path, string login)
        {
            try
            {
                if (Directory.Exists(cs2_path + "\\game\\csgo\\panorama\\videos"))
                {
                    Directory.Move(cs2_path + "\\game\\csgo\\panorama\\videos", cs2_path + $"\\game\\csgo\\panorama\\videos{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                }
            }
            catch (Exception ex)
            {

            }
            if (File.Exists(cs2_path + "\\game\\csgo\\maps\\de_vertigo_vanity.vpk"))
            {
                try
                {
                    File.Delete(cs2_path + "\\game\\csgo\\maps\\de_vertigo_vanity.vpk");
                }
                catch (Exception ex2)
                {

                }
            }
            try
            {
                if (!File.Exists(steam_path + "\\steam.cfg"))
                {
                    File.WriteAllLines(steam_path + "\\steam.cfg", new string[3] { "BootStrapperInhibitAll=enable", "BootStrapperForceSelfUpdate = disable", "#ForceOfflineMode=enable" });
                }
            }
            catch (Exception ex3)
            {

            }
            if (File.Exists(cs2_path + "\\game\\csgo\\cfg\\boost.cfg"))
            {
                File.Delete(cs2_path + "\\game\\csgo\\cfg\\boost.cfg");
            }
            File.WriteAllText(cs2_path + "\\game\\csgo\\cfg\\boost.cfg", SettingsManager.GetAutoExecFileSettings());
            try
            {
                Directory.CreateDirectory(steam_path + "\\userdata");
                Directory.CreateDirectory(steam_path + "\\userdata\\" + steam_id_32);
                Directory.CreateDirectory(steam_path + "\\userdata\\" + steam_id_32 + "\\730");
                Directory.CreateDirectory(steam_path + "\\userdata\\" + steam_id_32 + "\\config");
                Directory.CreateDirectory(steam_path + "\\userdata\\" + steam_id_32 + "\\730\\local");
                Directory.CreateDirectory(steam_path + "\\userdata\\" + steam_id_32 + "\\730\\local\\cfg");
                if (File.Exists(steam_path + "\\userdata\\" + steam_id_32 + "\\config\\localconfig.vdf"))
                {
                    SetFileReadAccess(steam_path + "\\userdata\\" + steam_id_32 + "\\config\\localconfig.vdf", setReadOnly: false);
                }
                File.WriteAllText(steam_path + "\\userdata\\" + steam_id_32 + "\\config\\localconfig.vdf", Encoding.UTF8.GetString(Convert.FromBase64String(LocalConfig)).Replace("????", $"-con_logfile {login}.log -allowmultiple -exec boost.cfg -language english " + SettingsManager.CS2Arguments));
                if (File.Exists(steam_path + "\\userdata\\" + steam_id_32 + "\\730\\local\\cfg\\cs2_video.txt"))
                {
                    SetFileReadAccess(steam_path + "\\userdata\\" + steam_id_32 + "\\730\\local\\cfg\\cs2_video.txt", setReadOnly: false);
                }
                File.WriteAllText(steam_path + "\\userdata\\" + steam_id_32 + "\\730\\local\\cfg\\cs2_video.txt", SettingsManager.GetVideoFileSettings());
                SetFileReadAccess(steam_path + "\\userdata\\" + steam_id_32 + "\\730\\local\\cfg\\cs2_video.txt", setReadOnly: true);
                if (File.Exists(steam_path + "\\userdata\\" + steam_id_32 + "\\730\\local\\cfg\\cs2_machine_convars.vcfg"))
                {
                    SetFileReadAccess(steam_path + "\\userdata\\" + steam_id_32 + "\\730\\local\\cfg\\cs2_machine_convars.vcfg", setReadOnly: false);
                }
                File.WriteAllText(steam_path + "\\userdata\\" + steam_id_32 + "\\730\\local\\cfg\\cs2_machine_convars.vcfg", SettingsManager.GetMachineConvarsFileSettings());
                SetFileReadAccess(steam_path + "\\userdata\\" + steam_id_32 + "\\730\\local\\cfg\\cs2_machine_convars.vcfg", setReadOnly: true);
                if (!File.Exists(cs2_path + "\\game\\csgo\\cfg\\gamestate_integration_GSI.cfg"))
                {
                    File.WriteAllText(cs2_path + "\\game\\csgo\\cfg\\gamestate_integration_GSI.cfg", Encoding.UTF8.GetString(Convert.FromBase64String(GameStateConfig)));
                }
            }
            catch (Exception ex4)
            {
            }
        }
        private static void SetFileReadAccess(string fileName, bool setReadOnly)
        {
            new FileInfo(fileName).IsReadOnly = setReadOnly;
        }
    }

}
