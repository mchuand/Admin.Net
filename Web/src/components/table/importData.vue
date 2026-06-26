<template>
	<div class="sys-import-data-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="300px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-UploadFilled /> </el-icon>
					<span> 数据导入 </span>
				</div>
			</template>
			
			<el-row :gutter="15" v-loading="state.loading">
				<el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12">
					<el-button class="ml10" type="info" icon="ele-Download" v-reclick="3000" @click="() => download()" :disabled="state.loading">模板</el-button>
				</el-col>
				<el-col :xs="12" :sm="12" :md="12" :lg="12" :xl="12">
					<el-upload
						:limit="1"
						:show-file-list="false"
						:on-exceed="handleExceed"
						:http-request="handleImportData"
						ref="uploadRef"
					>
						<template #trigger>
							<el-button type="primary" icon="ele-MostlyCloudy" v-reclick="3000" :disabled="state.loading">导入</el-button>
						</template>
					</el-upload>
				</el-col>
			</el-row>

			<template #footer>
				<span class="dialog-footer">
					<el-button @click="() => state.isShowDialog = false" :disabled="state.loading">取 消</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="sysImportData">
import type {UploadInstance, UploadProps, UploadRawFile, UploadRequestOptions} from 'element-plus'
import { ElUpload, ElMessage, genFileId } from 'element-plus';
import { downloadStreamFile } from '/@/utils/download';
import { reactive, ref } from 'vue';

const uploadRef = ref<UploadInstance>();
const state = reactive({
	isShowDialog: false,
	loading: false,
});

// 定义子组件向父组件传值/事件
const props = defineProps(['import', 'download']);
const emit = defineEmits(['refresh']);

// 打开弹窗
const openDialog = () => {
	state.isShowDialog = true;
};

// 选择文件超出上限事件
const handleExceed: UploadProps['onExceed'] = (files) => {
  uploadRef.value!.clearFiles();
  const file = files[0] as UploadRawFile;
  file.uid = genFileId();
  uploadRef.value!.handleStart(file);
}

// 数据导入
const handleImportData = (opt: UploadRequestOptions): any => {
  state.loading = true;
  props.import(opt.file).then((res: any) => {
    // 返回json数据的情况
	const contentType = res.headers['content-type'];
	if (contentType && contentType.toLowerCase().includes('application/json')) {
		const decoder = new TextDecoder('utf-8');
		const data = decoder.decode(res.data);
		try {
			const result = JSON.parse(data);
			if(result.code == '200'){
				ElMessage.success(result.message);
			} else {
				ElMessage.error(result.message);
				return;
			}
		} catch (e) {
			console.error("解析数据导入结果失败:", e);
			downloadStreamFile(res);
		}
	}
	else {
		downloadStreamFile(res);
	}
    emit('refresh');
    state.isShowDialog = false;
  }).finally(() => {
    uploadRef.value?.clearFiles();
    state.loading = false;
  });
}

// 下载模板
const download = () => {
	props.download().then((res: any) => downloadStreamFile(res)).catch((res: any) => ElMessage.error('下载错误: ' + res));
}

// 导出对象
defineExpose({ openDialog });
</script>
