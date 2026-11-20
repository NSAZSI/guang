using AdPulse.Api.Dtos;
using FluentValidation;

namespace AdPulse.Api.Validators;

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().WithMessage("用户名不能为空");
        RuleFor(x => x.Password).NotEmpty().WithMessage("密码不能为空");
    }
}

public class SaveNewsValidator : AbstractValidator<SaveNewsRequest>
{
    public SaveNewsValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("标题不能为空")
            .MaximumLength(200).WithMessage("标题不能超过 200 字");
        // 草稿允许摘要/封面暂空（便于先存稿再用 AI 生成摘要）；发布时必填
        RuleFor(x => x.Summary).NotEmpty().When(x => x.Publish).WithMessage("发布前请填写摘要");
        RuleFor(x => x.Summary).MaximumLength(300).WithMessage("摘要不能超过 300 字");
        RuleFor(x => x.Content).NotEmpty().WithMessage("正文不能为空");
        RuleFor(x => x.CoverUrl).NotEmpty().When(x => x.Publish).WithMessage("发布前请上传封面图");
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("请选择分类");
        RuleFor(x => x.TagIds).NotNull().WithMessage("标签列表不能为空");
    }
}

public class ChangeStatusValidator : AbstractValidator<ChangeStatusRequest>
{
    private static readonly string[] Allowed = { "publish", "unpublish", "top", "untop", "recycle", "restore" };
    public ChangeStatusValidator()
    {
        RuleFor(x => x.Action).NotEmpty().WithMessage("状态操作不能为空")
            .Must(a => Allowed.Contains(a)).WithMessage("不支持的状态操作");
    }
}

public class SaveCategoryValidator : AbstractValidator<SaveCategoryRequest>
{
    public SaveCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("分类名不能为空").MaximumLength(32).WithMessage("分类名不能超过 32 字");
        RuleFor(x => x.Slug).NotEmpty().WithMessage("Slug 不能为空")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug 只能包含小写字母、数字和短横线");
    }
}

public class SaveTagValidator : AbstractValidator<SaveTagRequest>
{
    public SaveTagValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("标签名不能为空").MaximumLength(32).WithMessage("标签名不能超过 32 字");
    }
}

public class SaveBannerValidator : AbstractValidator<SaveBannerRequest>
{
    public SaveBannerValidator()
    {
        RuleFor(x => x.ImageUrl).NotEmpty().WithMessage("请上传轮播图");
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0).WithMessage("排序值不能为负数");
    }
}