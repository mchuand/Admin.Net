import {useBaseApi} from '/@/api/base';

// 物料信息接口服务
export const useMaterielApi = () => {
	const baseApi = useBaseApi("materiel");
	return {
		// 分页查询物料信息
		page: baseApi.page,
		// 查看物料信息详细
		detail: baseApi.detail,
		// 新增物料信息
		add: baseApi.add,
		// 更新物料信息
		update: baseApi.update,
		// 删除物料信息
		delete: baseApi.delete,
		// 批量删除物料信息
		batchDelete: baseApi.batchDelete,
		// 导出物料信息数据
		exportData: baseApi.exportData,
		// 导入物料信息数据
		importData: baseApi.importData,
		// 下载物料信息数据导入模板
		downloadTemplate: baseApi.downloadTemplate,
		// 获取下拉列表数据
		getDropdownData: (fromPage: Boolean = false, cancel: boolean = false) => baseApi.dropdownData({ fromPage }, cancel),
	}
}

// 物料信息实体
export interface Materiel {
	// 主键Id
	id: number;
	// 创建时间
	createTime: string;
	// 更新时间
	updateTime: string;
	// 创建者Id
	createUserId: number;
	// 创建者姓名
	createUserName: string;
	// 修改者Id
	updateUserId: number;
	// 修改者姓名
	updateUserName: string;
	// 租户Id
	tenantId: number;
	// 物料类型
	type?: number;
	// 物料名称
	name?: string;
	// 物料全称
	fullName: string;
	// 物料规格
	spec?: string;
	// 单位
	uNIT?: string;
	// 状态（是否禁用）
	isEnable?: boolean;
	// 禁用人id
	enablerId: number;
	// 禁用人
	enabler: string;
	// 禁用时间
	enableTime: string;
	// 备注
	remark: string;
}