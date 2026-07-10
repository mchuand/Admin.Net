<template>
	<div class="sys-file-container">
		<el-card shadow="hover" :body-style="{ paddingBottom: '0' }" v-if="userStore.userInfos.accountType == 999">
			<el-form :inline="true">
				<el-form-item label="租户">
					<el-select v-model="state.queryParams.tenantId" placeholder="租户" style="width: 200px" @change="handleTenantChange">
						<el-option :value="item.value" :label="`${item.label} (${item.host})`" v-for="(item, index) in state.tenantList" :key="index" />
					</el-select>
				</el-form-item>
			</el-form>
		</el-card>

		<el-card class="full-table" header-class="card_header" shadow="hover" style="margin-top: 5px">
			<template #header>
				<ButtonBar mode="sysFile" :buttonConfig="fileButtonConfig" displayStyle="inline"
					:onButtonClick="handleButtonClick" />

				<AdvancedSearch ref="searchRef" :fields="searchFields" :keywordFields="keywordFields"
					mode="sysFile" :disableAutoQuery="true" @query="handleAdvancedQuery" @reset="handleAdvancedReset" />
			</template>

			<el-table ref="tableRef" :data="state.fileData" style="width: 100%" v-loading="state.loading" border
				@selection-change="handleSelectionChange" @row-click="handleRowClick">
				<el-table-column type="selection" width="55" align="center" fixed />
				<el-table-column type="index" label="序号" width="55" align="center" fixed />
				<el-table-column prop="fileName" label="名称" min-width="150" header-align="center" show-overflow-tooltip />
				<el-table-column prop="suffix" label="后缀" align="center" show-overflow-tooltip>
					<template #default="scope">
						<el-tag round>{{ scope.row.suffix }}</el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="sizeKb" label="大小kb" align="center" show-overflow-tooltip />
				<el-table-column prop="url" label="预览" align="center">
					<template #default="scope">
						<el-image
							style="width: 60px; height: 60px"
							:src="getFileUrl(scope.row)"
							alt="无法预览"
							:lazy="true"
							:hide-on-click-modal="true"
							:preview-src-list="[getFileUrl(scope.row)]"
							:initial-index="0"
							fit="scale-down"
							preview-teleported
						>
							<template #error> </template>
						</el-image>
					</template>
				</el-table-column>
				<el-table-column prop="bucketName" label="存储位置" align="center" show-overflow-tooltip />
				<el-table-column prop="id" label="存储标识" align="center" show-overflow-tooltip />
				<el-table-column prop="fileType" label="文件类型" min-width="100" header-align="center" show-overflow-tooltip />
				<el-table-column prop="isPublic" label="是否公开" min-width="100" header-align="center" show-overflow-tooltip>
					<template #default="scope">
						<el-tag v-if="scope.row.isPublic === true" type="success">是</el-tag>
						<el-tag v-else type="danger">否</el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="relationName" label="关联对象名称" min-width="150" align="center" />
				<el-table-column prop="relationId" label="关联对象Id" align="center" />
				<el-table-column prop="belongId" label="所属Id" align="center" />
				<el-table-column label="修改记录" width="100" align="center" show-overflow-tooltip>
					<template #default="scope">
						<ModifyRecord :data="scope.row" />
					</template>
				</el-table-column>
				<el-table-column label="操作" width="260" fixed="right" align="center" show-overflow-tooltip>
					<template #default="scope">
						<el-button-group>
							<el-button icon="ele-View" size="small" type="primary" @click="openFilePreviewDialog(scope.row)" v-auth="'sysFile:delete'"></el-button>
							<el-button icon="ele-Download" size="small" type="primary" @click="downloadFile(scope.row)" v-auth="'sysFile:downloadFile'"></el-button>
							<el-button icon="ele-Delete" size="small" type="danger" @click="delFile(scope.row)" v-auth="'sysFile:delete'"></el-button>
							<el-button icon="ele-Edit" size="small" type="primary" @click="openEditSysFile(scope.row)" v-auth="'sysFile:update'"></el-button>
						</el-button-group>
					</template>
				</el-table-column>
			</el-table>
			<el-pagination
				v-model:currentPage="state.tableParams.page"
				v-model:page-size="state.tableParams.pageSize"
				:total="state.tableParams.total"
				:page-sizes="[10, 20, 50, 100]"
				size="small"
				background
				@size-change="handleSizeChange"
				@current-change="handleCurrentChange"
				layout="total, sizes, prev, pager, next, jumper"
			/>
		</el-card>

		<el-dialog v-model="state.dialogUploadVisible" :lock-scroll="false" draggable width="400px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-UploadFilled /> </el-icon>
					<span> 上传文件 </span>
				</div>
			</template>
			<div>
				<el-select v-model="state.fileType" placeholder="请选择文件类型" style="margin-bottom: 10px">
					<el-option label="相关文件" value="相关文件" />
					<el-option label="归档文件" value="归档文件" />
				</el-select>
				是否公开：
				<el-radio-group v-model="state.isPublic">
					<el-radio :value="false">否</el-radio>
					<el-radio :value="true">是</el-radio>
				</el-radio-group>
				<el-upload ref="uploadRef" drag :auto-upload="false" :limit="1" :file-list="state.fileList" action :on-change="handleChange" accept=".jpg,.png,.bmp,.gif,.txt,.xml,.pdf,.xlsx,.docx">
					<el-icon class="el-icon--upload">
						<ele-UploadFilled />
					</el-icon>
					<div class="el-upload__text">将文件拖到此处，或<em>点击上传</em></div>
					<template #tip>
						<div class="el-upload__tip">请上传大小不超过 10MB 的文件</div>
					</template>
				</el-upload>
			</div>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="state.dialogUploadVisible = false">取消</el-button>
					<el-button type="primary" @click="uploadFile">确定</el-button>
				</span>
			</template>
		</el-dialog>

		<el-drawer :title="state.fileName" v-model="state.dialogDocxVisible" size="50%" destroy-on-close>
			<vue-office-docx :src="state.docxUrl" style="height: 100vh" @rendered="renderedHandler" @error="errorHandler" />
		</el-drawer>
		<el-drawer :title="state.fileName" v-model="state.dialogXlsxVisible" size="50%" destroy-on-close>
			<vue-office-excel :src="state.excelUrl" style="height: 100vh" @rendered="renderedHandler" @error="errorHandler" />
		</el-drawer>
		<el-drawer :title="state.fileName" v-model="state.dialogPdfVisible" size="50%" destroy-on-close>
			<vue-office-pdf :src="state.pdfUrl" style="height: 100vh" @rendered="renderedHandler" @error="errorHandler" />
		</el-drawer>
		<el-image-viewer v-if="state.showViewer" :url-list="state.previewList" :hideOnClickModal="true" @close="state.showViewer = false"></el-image-viewer>
		<EditSysFile ref="editSysFileRef" title="编辑文件" @handleQuery="handleAdvancedQuery(state.advancedConditions)" />
	</div>
</template>

<script lang="ts" setup name="sysFile">
import { onMounted, reactive, ref, shallowRef } from 'vue';
import { ElMessageBox, ElMessage, UploadInstance } from 'element-plus';
import VueOfficeDocx from '@vue-office/docx';
import VueOfficeExcel from '@vue-office/excel';
import VueOfficePdf from '@vue-office/pdf';
import '@vue-office/docx/lib/index.css';
import '@vue-office/excel/lib/index.css';

import EditSysFile from '/@/views/system/file/component/editSysfile.vue';
import ModifyRecord from '/@/components/table/modifyRecord.vue';
import ButtonBar from '/@/components/buttonBar/index.vue';
import AdvancedSearch from '/@/components/advancedSearch/index.vue';
import type { SearchField, QueryCondition } from '/@/components/advancedSearch/types';

import { downloadByUrl } from '/@/utils/download';
import { getAPI } from '/@/utils/axios-utils';
import {SysFileApi, SysTenantApi} from '/@/api-services/api';
import { SysFile } from '/@/api-services/models';
import { useUserInfo } from "/@/stores/userInfo";

const userStore = useUserInfo();
const uploadRef = ref<UploadInstance>();
const editSysFileRef = ref<InstanceType<typeof EditSysFile>>();
const searchRef = ref();
const tableRef = ref();
const selectRows = shallowRef<any[]>([]);

const searchFields: SearchField[] = [
	{ label: '文件名称', prop: 'fileName', type: 'string' },
	{ label: '创建时间', prop: 'createTime', type: 'datetimeRange' },
];

const keywordFields = ['fileName'];

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
	tenantList: [] as Array<any>,
	fileData: [] as Array<SysFile>,
	queryParams: {
		tenantId: undefined as number | undefined,
	},
	advancedConditions: [] as QueryCondition[],
	tableParams: {
		page: 1,
		pageSize: 50,
		total: 0 as any,
	},
	dialogUploadVisible: false,
	diaglogEditFile: false,
	fileList: [] as any,
	dialogDocxVisible: false,
	dialogXlsxVisible: false,
	dialogPdfVisible: false,
	showViewer: false,
	docxUrl: '',
	excelUrl: '',
	pdfUrl: '',
	fileName: '',
	fileType: '',
	isPublic: false,
	previewList: [] as string[],
});

const fileButtonConfig = {
	base: {
		type: 'group' as const,
		childs: {
			uploadFile: { type: 'button' as const, label: '上传', icon: 'ele-Upload', color: 'primary' as const },
			update: { type: 'button' as const, label: '编辑', icon: 'ele-Edit', color: 'success' as const },
			delete: { type: 'button' as const, label: '删除', icon: 'ele-Delete', color: 'danger' as const },
		}
	},
};

const handleButtonClick = (key: string) => {
	switch (key) {
		case 'uploadFile': openUploadDialog(); break;
		case 'update': handleBatchUpdate(); break;
		case 'delete': handleBatchDelete(); break;
	}
};

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

const handleBatchUpdate = () => {
	if (!validateSelection(1, 1)) return;
	openEditSysFile(selectRows.value[0]);
};

const handleBatchDelete = () => {
	if (!validateSelection()) return;
	const fileNames = selectRows.value.map((r: any) => r.fileName).join('、');
	ElMessageBox.confirm(`确定要删除文件「${fileNames}」吗?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	}).then(async () => {
		state.loading = true;
		const ids = selectRows.value.map((r: any) => r.id);
		await Promise.all(ids.map((id: any) => getAPI(SysFileApi).apiSysFileDeletePost({ id })));
		await handleAdvancedQuery(state.advancedConditions);
		selectRows.value = [];
		ElMessage.success('删除成功');
	}).catch(() => { });
};

onMounted(async () => {
	if (userStore.userInfos.accountType == 999) {
		state.tenantList = await getAPI(SysTenantApi).apiSysTenantListGet().then(res => res.data.result ?? []);
		if (state.tenantList.length > 0) {
			state.queryParams.tenantId = state.tenantList[0].value;
		}
	}
	await handleAdvancedQuery([]);
});

const handleTenantChange = () => {
	handleAdvancedQuery(state.advancedConditions);
};

const handleAdvancedQuery = async (conditions: QueryCondition[]) => {
	state.advancedConditions = conditions;
	state.loading = true;

	const keywordValue = searchRef.value?.getKeyword?.() || '';

	let params: any = {
		page: state.tableParams.page,
		pageSize: state.tableParams.pageSize,
		keyword: keywordValue,
		keywordFields: keywordFields,
		conditions: conditions,
		tenantId: state.queryParams.tenantId,
	};

	try {
		let res = await getAPI(SysFileApi).apiSysFilePageAdvancedPost(params);
		state.fileData = res.data.result?.items ?? [];
		state.tableParams.total = res.data.result?.total;
	} catch (error) {
		console.error('查询失败:', error);
	}
	state.loading = false;
};

const handleAdvancedReset = async (conditions: QueryCondition[]) => {
	state.advancedConditions = [];
	await handleAdvancedQuery([]);
};

const openUploadDialog = () => {
	state.fileList = [];
	state.dialogUploadVisible = true;
	state.isPublic = false;
};

const handleChange = (file: any, fileList: []) => {
	state.fileList = fileList;
};

const uploadFile = async () => {
	if (state.fileList.length < 1) return;
	await getAPI(SysFileApi).apiSysFileUploadFilePostForm(state.fileList[0].raw, state.fileType, state.isPublic, undefined);
	handleAdvancedQuery(state.advancedConditions);
	ElMessage.success('上传成功');
	state.dialogUploadVisible = false;
};

const downloadFile = async (row: any) => {
	var fileUrl = getFileUrl(row);
	downloadByUrl({ url: fileUrl });
};

const delFile = (row: any) => {
	ElMessageBox.confirm(`确定删除文件：【${row.fileName}】?`, '提示', {
		confirmButtonText: '确定',
		cancelButtonText: '取消',
		type: 'warning',
	})
		.then(async () => {
			await getAPI(SysFileApi).apiSysFileDeletePost({ id: row.id });
			handleAdvancedQuery(state.advancedConditions);
			ElMessage.success('删除成功');
		})
		.catch(() => {});
};

const openFilePreviewDialog = async (row: any) => {
	if (row.suffix == '.pdf') {
		state.fileName = `【${row.fileName}${row.suffix}】`;
		state.pdfUrl = getFileUrl(row);
		state.dialogPdfVisible = true;
	} else if (row.suffix == '.docx') {
		state.fileName = `【${row.fileName}${row.suffix}】`;
		state.docxUrl = getFileUrl(row);
		state.dialogDocxVisible = true;
	} else if (row.suffix == '.xlsx') {
		state.fileName = `【${row.fileName}${row.suffix}】`;
		state.excelUrl = getFileUrl(row);
		state.dialogXlsxVisible = true;
	} else if (['.jpg', '.png', '.jpeg', '.bmp'].findIndex((e) => e == row.suffix) > -1) {
		state.previewList = [getFileUrl(row)];
		state.showViewer = true;
	} else {
		ElMessage.error('此文件格式不支持预览');
	}
};

const handleSizeChange = async (val: number) => {
	state.tableParams.pageSize = val;
	if (state.advancedConditions.length > 0) {
		await handleAdvancedQuery(state.advancedConditions);
	} else {
		await handleAdvancedQuery([]);
	}
};

const handleCurrentChange = async (val: number) => {
	state.tableParams.page = val;
	if (state.advancedConditions.length > 0) {
		await handleAdvancedQuery(state.advancedConditions);
	} else {
		await handleAdvancedQuery([]);
	}
};

const getFileUrl = (row: SysFile): string => {
	if (row.bucketName == 'Local') {
		return `/${row.filePath}/${row.id}${row.suffix}`;
	} else {
		return row.url!;
	}
};

const openEditSysFile = (row: any) => {
	editSysFileRef.value?.openDialog(row);
};

const renderedHandler = () => {};
const errorHandler = () => {};
</script>

<style scoped lang="scss">
.sys-file-container {
	height: 100%;
}

:deep(.card_header) {
	padding: 0 3px 3px 3px;
}
</style>
