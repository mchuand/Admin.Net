<template>
	<div class="sys-notice-container">
		<el-card class="full-table" header-class="card_header" shadow="hover" style="margin-top: 5px">
			<template #header>
				<!-- 按钮栏组件 -->
				<ButtonBar mode="sysNotice" :buttonConfig="noticeButtonConfig" displayStyle="inline"
					:onButtonClick="handleButtonClick" />

				<!-- 高级查询组件 -->
				<AdvancedSearch ref="searchRef" :fields="searchFields" :keywordFields="keywordFields" mode="sysNotice"
					:disableAutoQuery="true" @query="handleAdvancedQuery" @reset="handleAdvancedReset" />
			</template>

			<el-table ref="tableRef" :data="state.noticeData" v-loading="state.loading" border
				@selection-change="handleSelectionChange" @row-click="handleRowClick">
				<el-table-column type="selection" width="55" align="center" fixed />
				<el-table-column type="index" label="序号" width="55" align="center" />
				<el-table-column prop="title" label="标题" header-align="center" show-overflow-tooltip />
				<el-table-column prop="content" label="内容" header-align="center" show-overflow-tooltip>
					<template #default="scope"> {{ removeHtml(scope.row.content) }} </template>
				</el-table-column>
				<el-table-column prop="type" label="类型" width="100" align="center" show-overflow-tooltip>
					<template #default="scope">
						<g-sys-dict v-model="scope.row.type" code="NoticeTypeEnum" />
					</template>
				</el-table-column>
				<el-table-column prop="createTime" label="创建时间" align="center" show-overflow-tooltip />
				<el-table-column prop="status" label="状态" width="100" align="center" show-overflow-tooltip>
					<template #default="scope">
						<g-sys-dict v-model="scope.row.status" code="NoticeStatusEnum" />
					</template>
				</el-table-column>
				<el-table-column prop="publicUserName" label="发布者" align="center" show-overflow-tooltip />
				<el-table-column prop="publicTime" label="发布时间" align="center" show-overflow-tooltip />
			</el-table>
			<el-pagination v-model:currentPage="state.tableParams.page" v-model:page-size="state.tableParams.pageSize"
				:total="state.tableParams.total" :page-sizes="[10, 20, 50, 100]" size="small" background
				@size-change="handleSizeChange" @current-change="handleCurrentChange"
				layout="total, sizes, prev, pager, next, jumper" />
		</el-card>

		<EditNotice ref="editNoticeRef" :title="state.editNoticeTitle"
			@handleQuery="handleAdvancedQuery(state.advancedConditions)" />
	</div>
</template>

<script lang="ts" setup name="sysNotice">
import { onMounted, reactive, ref, shallowRef } from 'vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import { getAPI } from '/@/utils/axios-utils';
import { SysNoticeApi } from '/@/api-services/api';
import { SysNotice } from '/@/api-services/models';
import commonFunction from '/@/utils/commonFunction';
import EditNotice from '/@/views/system/notice/component/editNotice.vue';
import ButtonBar from '/@/components/buttonBar/index.vue';
import AdvancedSearch from '/@/components/advancedSearch/index.vue';
import type { SearchField, QueryCondition } from '/@/components/advancedSearch/types';

const editNoticeRef = ref<InstanceType<typeof EditNotice>>();
const searchRef = ref();
const tableRef = ref();
const selectRows = shallowRef<any[]>([]);
const { removeHtml } = commonFunction();

// 搜索字段配置
const searchFields: SearchField[] = [
	{ label: '标题', prop: 'title', type: 'string' },
	{ label: '类型', prop: 'type', type: 'dic', dicCode: 'NoticeTypeEnum' },
];

// 关键字搜索字段列表
const keywordFields = ['title'];

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
	noticeData: [] as Array<SysNotice>,
	advancedConditions: [] as QueryCondition[],
	tableParams: {
		page: 1,
		pageSize: 50,
		total: 0 as any,
	},
	editNoticeTitle: '',
});

// 按钮栏配置
const noticeButtonConfig = {
	base: {
		type: 'group' as const,
		childs: {
			add: { type: 'button' as const, label: '新增', icon: 'ele-Plus', color: 'primary' as const },
			update: { type: 'button' as const, label: '修改', icon: 'ele-Edit', color: 'success' as const },
			delete: { type: 'button' as const, label: '删除', icon: 'ele-Delete', color: 'danger' as const }
		}
	},
	options: {
		type: 'group' as const,
		childs: {
			public: { type: 'button' as const, label: '发布', icon: 'ele-Position', color: 'warning' as const }
		}
	}
};

// 按钮栏点击事件
const handleButtonClick = (key: string) => {
	switch (key) {
		case 'add': openAddNotice(); break;
		case 'update': handleBatchUpdate(); break;
		case 'delete': handleBatchDelete(); break;
		case 'public': handleBatchPublic(); break;
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
	openEditNotice(selectRows.value[0]);
};

// 批量删除
const handleBatchDelete = () => {
	if (!validateSelection()) return;
	const names = selectRows.value.map((r: any) => r.title).join('、');
	ElMessageBox.confirm(`确定要删除通知「${names}」吗?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(async () => {
		state.loading = true;
		const ids = selectRows.value.map((r: any) => r.id);
		await Promise.all(ids.map((id: any) => getAPI(SysNoticeApi).apiSysNoticeDeletePost({ id })));
		await handleAdvancedQuery(state.advancedConditions);
		selectRows.value = [];
		ElMessage.success('删除成功');
	}).catch(() => { });
};

// 批量发布
const handleBatchPublic = () => {
	if (!validateSelection(1)) return;
	const names = selectRows.value.map((r: any) => r.title).join('、');
	ElMessageBox.confirm(`确定要发布通知「${names}」吗? 不可撤销!`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(async () => {
		state.loading = true;
		const ids = selectRows.value.map((r: any) => r.id);
		await Promise.all(ids.map((id: any) => getAPI(SysNoticeApi).apiSysNoticePublicPost({ id })));
		await handleAdvancedQuery(state.advancedConditions);
		selectRows.value = [];
		ElMessage.success('发布成功');
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
		let res = await getAPI(SysNoticeApi).apiSysNoticePageAdvancedPost(params as any);
		state.noticeData = res.data.result?.items ?? [];
		state.tableParams.total = res.data.result?.total;
	} catch (error) {
		console.error('查询失败:', error);
	}
	state.loading = false;
};

// 高级查询重置
const handleAdvancedReset = async (conditions: QueryCondition[]) => {
	state.tableParams.page = 1;
	state.advancedConditions = [];
	await handleAdvancedQuery([]);
};

// 打开新增页面
const openAddNotice = () => {
	state.editNoticeTitle = '添加通知公告';
	editNoticeRef.value?.openDialog({ type: 1 });
};

// 打开编辑页面
const openEditNotice = (row: any) => {
	state.editNoticeTitle = '编辑通知公告';
	editNoticeRef.value?.openDialog(row);
};

// 改变页面容量
const handleSizeChange = async (val: number) => {
	state.tableParams.pageSize = val;
	await handleAdvancedQuery(state.advancedConditions);
};

// 改变页码序号
const handleCurrentChange = async (val: number) => {
	state.tableParams.page = val;
	await handleAdvancedQuery(state.advancedConditions);
};
</script>

<style scoped lang="scss">
.sys-notice-container {
	height: 100%;
}

:deep(.card_header) {
	padding: 0 3px 3px 3px;
}
</style>
