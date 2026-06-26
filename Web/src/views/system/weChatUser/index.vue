<template>
	<div class="weChatUser-container">

		<el-card class="full-table" header-class="card_header" shadow="hover" style="margin-top: 5px">
			<template #header>
				<!-- 按钮栏组件 -->
				<ButtonBar mode="sysWechatUser" :buttonConfig="wechatUserButtonConfig" displayStyle="inline"
					:onButtonClick="handleButtonClick" />

				<!-- 高级查询组件 -->
				<AdvancedSearch ref="searchRef" :fields="searchFields" :keywordFields="keywordFields"
					mode="sysWechatUser" :disableAutoQuery="true" @query="handleAdvancedQuery" @reset="handleAdvancedReset" />
			</template>

			<el-table ref="tableRef" :data="state.weChatUserData" style="width: 100%" v-loading="state.loading" border
				@selection-change="handleSelectionChange" @row-click="handleRowClick">
				<el-table-column type="selection" width="55" align="center" fixed />
				<el-table-column type="index" label="序号" width="55" align="center" fixed />
				<el-table-column prop="openId" label="OpenId" align="center" show-overflow-tooltip />
				<el-table-column prop="unionId" label="UnionId" align="center" show-overflow-tooltip />
				<el-table-column prop="platformType" label="平台类型" width="110" align="center" show-overflow-tooltip>
					<template #default="scope">
						<g-sys-dict v-model="scope.row.platformType" code="PlatformTypeEnum" default-value="其他" />
					</template>
				</el-table-column>
				<el-table-column prop="nickName" label="昵称" align="center" show-overflow-tooltip />
				<el-table-column prop="avatar" label="头像" width="70" align="center">
					<template #default="scope">
						<el-avatar :src="scope.row.avatar" :size="24" style="vertical-align: middle" />
					</template>
				</el-table-column>
				<el-table-column prop="mobile" label="手机号码" align="center" show-overflow-tooltip />
				<el-table-column prop="sex" label="性别" width="60" align="center" show-overflow-tooltip>
					<template #default="scope">
						<el-tag v-if="scope.row.sex === 0"> 男 </el-tag>
						<el-tag type="danger" v-else> 女 </el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="city" label="城市" align="center" show-overflow-tooltip />
				<el-table-column prop="province" label="省" align="center" show-overflow-tooltip />
				<el-table-column prop="country" label="国家" align="center" show-overflow-tooltip />
			</el-table>
			<el-pagination v-model:currentPage="state.tableParams.page"
				v-model:page-size="state.tableParams.pageSize" :total="state.tableParams.total"
				:page-sizes="[10, 20, 50, 100]" size="small" background @size-change="handleSizeChange"
				@current-change="handleCurrentChange" layout="total, sizes, prev, pager, next, jumper" />
		</el-card>
		<EditWeChatUser ref="editWeChatUserRef" :title="state.editWeChatUserTitle" @handleQuery="handleAdvancedQuery(state.advancedConditions)" />
	</div>
</template>

<script lang="ts" setup name="weChatUser">
import { onMounted, reactive, ref, shallowRef } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import EditWeChatUser from '/@/views/system/weChatUser/component/editWeChatUser.vue';
import ButtonBar from '/@/components/buttonBar/index.vue';
import AdvancedSearch from '/@/components/advancedSearch/index.vue';
import type { SearchField, QueryCondition } from '/@/components/advancedSearch/types';
import { getAPI } from '/@/utils/axios-utils';
import { SysWechatUserApi } from '/@/api-services/api';
import { SysWechatUser } from '/@/api-services/models';

const editWeChatUserRef = ref<InstanceType<typeof EditWeChatUser>>();
const searchRef = ref();
const tableRef = ref();
const selectRows = shallowRef<any[]>([]);

// 搜索字段配置
const searchFields: SearchField[] = [
	{ label: '微信昵称', prop: 'nickName', type: 'string' },
	{ label: '手机号码', prop: 'mobile', type: 'string' },
];

// 关键字搜索字段列表
const keywordFields = ['nickName', 'mobile'];

// 表格选中
const handleRowClick = (row: any) => {
	const table = tableRef.value;
	if (!table) return;
	table.toggleRowSelection(row);
};

const handleSelectionChange = (rows: any[]) => {
	selectRows.value = rows;
};

const state = reactive({
	loading: false,
	weChatUserData: [] as Array<SysWechatUser>,
	advancedConditions: [] as QueryCondition[],
	tableParams: {
		page: 1,
		pageSize: 50,
		total: 0 as any,
	},
	editWeChatUserTitle: '',
});

// 按钮栏配置（无新增按钮 - 微信用户通过登录创建）
const wechatUserButtonConfig = {
	base: {
		type: 'group' as const,
		childs: {
			update: { type: 'button' as const, label: '修改', icon: 'ele-Edit', color: 'success' as const },
			delete: { type: 'button' as const, label: '删除', icon: 'ele-Delete', color: 'danger' as const },
		}
	},
};

// 按钮栏点击事件
const handleButtonClick = (key: string) => {
	switch (key) {
		case 'update': handleBatchUpdate(); break;
		case 'delete': handleBatchDelete(); break;
	}
};

// 校验选中行
const validateSelection = (minCount = 1, maxCount?: number): boolean => {
	if (selectRows.value.length < minCount) {
		ElMessage.warning(`请至少选择${minCount}条记录`);
		return false;
	}
	if (maxCount && selectRows.value.length > maxCount) {
		ElMessage.warning(`最多选择${maxCount}条记录`);
		return false;
	}
	return true;
};

// 批量编辑
const handleBatchUpdate = () => {
	if (!validateSelection(1, 1)) return;
	openEditWeChatUser(selectRows.value[0]);
};

// 批量删除
const handleBatchDelete = () => {
	if (!validateSelection()) return;
	const names = selectRows.value.map((r: any) => r.nickName).join('、');
	ElMessageBox.confirm(`确定要删除微信用户「${names}」吗?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(async () => {
		state.loading = true;
		const ids = selectRows.value.map((r: any) => r.id);
		await Promise.all(ids.map((id: any) => getAPI(SysWechatUserApi).apiSysWechatUserDeletePost({ id })));
		await handleAdvancedQuery(state.advancedConditions);
		selectRows.value = [];
		ElMessage.success('删除成功');
	}).catch(() => { });
};

onMounted(async () => {
	await handleAdvancedQuery([]);
});

// 高级查询（所有查询统一走此方法）
const handleAdvancedQuery = async (conditions: QueryCondition[]) => {
	state.advancedConditions = conditions;
	state.loading = true;

	const keywordValue = searchRef.value?.getKeyword?.() || '';

	let params = {
		page: state.tableParams.page,
		pageSize: state.tableParams.pageSize,
		keyword: keywordValue,
		keywordFields: keywordFields,
		conditions: conditions
	};

	try {
		let res = await getAPI(SysWechatUserApi).apiSysWechatUserPageAdvancedPost(params as any);
		state.weChatUserData = res.data.result?.items ?? [];
		state.tableParams.total = res.data.result?.total;
	} catch (error) {
		console.error('查询失败:', error);
	}
	state.loading = false;
};

// 高级查询重置
const handleAdvancedReset = async (conditions: QueryCondition[]) => {
	state.advancedConditions = [];
	await handleAdvancedQuery([]);
};

// 打开编辑页面
const openEditWeChatUser = (row: any) => {
	state.editWeChatUserTitle = '编辑微信账号';
	editWeChatUserRef.value?.openDialog(row);
};

// 改变页面容量
const handleSizeChange = async (val: number) => {
	state.tableParams.pageSize = val;
	if (state.advancedConditions.length > 0) {
		await handleAdvancedQuery(state.advancedConditions);
	} else {
		await handleAdvancedQuery([]);
	}
};

// 改变页码序号
const handleCurrentChange = async (val: number) => {
	state.tableParams.page = val;
	if (state.advancedConditions.length > 0) {
		await handleAdvancedQuery(state.advancedConditions);
	} else {
		await handleAdvancedQuery([]);
	}
};
</script>

<style scoped lang="scss">
.weChatUser-container {
	height: 100%;
}

:deep(.card_header) {
	padding: 0 3px 3px 3px;
}
</style>
