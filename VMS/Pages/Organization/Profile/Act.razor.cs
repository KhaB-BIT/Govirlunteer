using VMS.Application.Interfaces;
using Microsoft.AspNetCore.Components;
using VMS.Application.ViewModels;
using System.Threading.Tasks;
using Blazored.Modal;
using System.Collections.Generic;
using Microsoft.JSInterop;
using Blazored.Modal.Services;
using VMS.Domain.Models;
using System.Linq;
using System;

namespace VMS.Pages.Organization.Profile
{
    public partial class Act : ComponentBase
    {
        private User currentUser;

        [Parameter] public bool HaveControl { get; set; }
        [Parameter] public bool HaveFav { get; set; }
        [Parameter] public bool HaveDecor { get; set; }
        [Parameter] public bool HaveLogin { get; set; }
        [Parameter] public string Title { get; set; }
        [Parameter] public bool HaveHeart { get; set; } = false;
        [Parameter] public bool OverridesImg { get; set; } = false;
        [Parameter] public bool HaveBorder { get; set; } = true;
        [Parameter] public bool HaveLinkAll { get; set; } = false;
        [Parameter] public bool Owner { get; set; }
        [Parameter] public string QueryString { get; set; }
        [Parameter] public bool IsHomepage { get; set; } = true;
        [Parameter] public bool IsOrgProfile { get; set; } = true;
        [Parameter] public bool IsUser { get; set; } = true;
        [Parameter] public string TitleLinkALl { get; set; }
        [Parameter] public bool IsUsedForAdmin { get; set; }

        [Parameter] public List<ActivityViewModel> Datas { get; set; }

        [CascadingParameter] private string CurrentUserId { get; set; }
        [CascadingParameter] public IModalService Modal { get; set; }

        [Inject]
        private IActivityService ActivityService { get; set; }
        [Inject]
        private IIdentityService IdentityService { get; set; }
        [Inject]
        private NavigationManager NavigationManager { get; set; }

        protected override void OnParametersSet()
        {
            currentUser = IdentityService.GetUserWithFavoritesAndRecruitmentsById(CurrentUserId);
            if (currentUser is not null && Datas is not null)
            {
                Datas.ForEach(a => a.IsFav = currentUser.Favorites.Any(x => x.ActivityId == a.Id));
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender && Datas.Count > 0)
            {
                await JS.InvokeVoidAsync("vms.OrganizeCarousel");
                await JS.InvokeVoidAsync("vms.AddOutsideClickMenuHandler", DotNetObjectReference.Create(this), nameof(HideMenuInterop));
            }
        }

        void ShowMenu(int id)
        {
            Datas.ForEach(a => a.IsMenu = a.Id == id && !a.IsMenu);
        }

        private void AnimateHeart(ActivityViewModel activity)
        {
            Favorite favorite = currentUser.Favorites.FirstOrDefault(f => f.ActivityId == activity.Id);

            if (favorite is null)
            {
                currentUser.Favorites.Add(new()
                {
                    UserId = CurrentUserId,
                    ActivityId = activity.Id,
                    CreatedDate = DateTime.Now
                });
            }
            else
            {
                currentUser.Favorites.Remove(favorite);
            }

            activity.IsFav = !activity.IsFav;

            IdentityService.UpdateUser(currentUser);
        }

        [JSInvokable]
        public Task HideMenuInterop()
        {
            Datas.ForEach(a => a.IsMenu = false);
            return InvokeAsync(StateHasChanged);
        }

        private async Task ShowDeleteModalAsync(int id)
        {
            var options = new ModalOptions()
            {
                HideCloseButton = true,
                DisableBackgroundCancel = true,
                UseCustomLayout = true,
            };

            var result = await Modal.Show<DeleteConfirm>("", options).Result;

            if ((bool)result.Data)
            {
                var act = Datas.Find(a => a.Id == id);
                await ActivityService.CloseOrDeleteActivity(id, true, act.IsClosed);
                Datas.Remove(act);
            }
        }

        private async Task ShowCloseModalAsync(int id)
        {
            var act = Datas.Find(a => a.Id == id);

            var parameters = new ModalParameters();
            parameters.Add("IsClosed", act.IsClosed);

            var options = new ModalOptions()
            {
                HideCloseButton = true,
                DisableBackgroundCancel = true,
                UseCustomLayout = true,
            };

            var result = await Modal.Show<CloseConfirm>("", parameters, options).Result;

            if ((bool)result.Data)
            {
                await ActivityService.CloseOrDeleteActivity(id, act.IsDeleted, !act.IsClosed);
                act.IsClosed = !act.IsClosed;
                Modal.Show<CloseSuccess>("", parameters, options);
            }
        }

        private void ShowLoginRequire()
        {

            var options = new ModalOptions()
            {
                HideCloseButton = true,
                DisableBackgroundCancel = true,
                UseCustomLayout = true,
            };

            Modal.Show<LoginRequire>("", options);
        }

    }
}