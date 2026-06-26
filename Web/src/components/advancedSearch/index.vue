<template>
	<div class="advanced-search-container">
		<el-card shadow="hover" :body-style="{ paddingBottom: '0' }">
			<el-form :model="queryParams" ref="formRef" inline>
				<!-- 设置按钮 -->
				<el-form-item class="setting-item">
					<el-tooltip content="设置查询条件" placement="top">
						<el-button circle icon="ele-Setting" @click="openSetting" />
					</el-tooltip>
				</el-form-item>

				<!-- 关键字搜索输入框（当配置了 keywordFields 且设置为可见时显示在最前面） -->
				<el-form-item v-if="hasKeywordFields && keywordVisible" label="关键字">
					<el-input
						v-model="keyword"
						placeholder="请输入关键字搜索"
						clearable
						@keyup.enter="handleQuery"
						style="width: 200px"
					/>
				</el-form-item>

				<!-- 查询字段 -->
				<el-form-item v-for="field in visibleFields" :key="field.prop" :label="field.label">
					<!-- 字符串类型 -->
					<el-input
						v-if="field.type === 'string' || !field.type"
						v-model="queryParams[field.prop]"
						:placeholder="field.placeholder || `请输入${field.label}`"
						clearable
						@keyup.enter="handleQuery"
						style="width: 160px"
					/>

					<!-- 数字类型 -->
					<el-input-number
						v-else-if="field.type === 'number'"
						v-model="queryParams[field.prop]"
						:placeholder="field.placeholder || `请输入${field.label}`"
						:controls="false"
						style="width: 120px"
					/>

					<!-- 数字范围 -->
					<template v-else-if="field.type === 'numberRange'">
						<el-input-number
							v-model="queryParams[`${field.prop}Start`]"
							placeholder="最小值"
							:controls="false"
							style="width: 90px"
						/>
						<span style="margin: 0 5px; color: #c0c4cc">~</span>
						<el-input-number
							v-model="queryParams[`${field.prop}End`]"
							placeholder="最大值"
							:controls="false"
							style="width: 90px"
						/>
					</template>

					<!-- 日期类型 -->
					<el-date-picker
						v-else-if="field.type === 'date'"
						v-model="queryParams[field.prop]"
						type="date"
						:placeholder="field.placeholder || `请选择`"
						value-format="YYYY-MM-DD"
						style="width: 140px"
					/>

					<!-- 日期范围 -->
					<el-date-picker
						v-else-if="field.type === 'dateRange'"
						v-model="queryParams[field.prop]"
						type="daterange"
						range-separator="~"
						start-placeholder="开始"
						end-placeholder="结束"
						value-format="YYYY-MM-DD"
						style="width: 240px"
					/>

					<!-- 字典单选 -->
					 <template v-else-if="field.type === 'dic'">
						<g-sys-dict v-model="queryParams[field.prop]" :code="field.dicCode" renderAs="select"/>

					<!-- <el-select
						v-else-if="field.type === 'dic'"
						v-model="queryParams[field.prop]"
						:placeholder="field.placeholder || `请选择`"
						clearable
						style="width: 120px"
					>
						<el-option
							v-for="item in getDicOptions(field)"
							:key="item.value"
							:label="item.label"
							:value="item.value"
						/>
					</el-select> -->
					 </template>

					<!-- 字典多选 -->
					 <template v-else-if="field.type === 'dicRange'">
						<g-sys-dict v-model="queryParams[field.prop]" :code="field.dicCode" renderAs="select" :multiple="true"/>
					 </template>
					<!-- <el-select
						v-model="queryParams[field.prop]"
						:placeholder="field.placeholder || `请选择`"
						clearable
						multiple
						collapse-tags
						collapse-tags-tooltip
						style="width: 180px"
					>
						<el-option
							v-for="item in getDicOptions(field)"
							:key="item.value"
							:label="item.label"
							:value="item.value"
						/>
					</el-select> -->

					<!-- 下拉选择（静态选项） -->
					<el-select
						v-else-if="field.type === 'select'"
						v-model="queryParams[field.prop]"
						:placeholder="field.placeholder || `请选择`"
						clearable
						style="width: 120px"
					>
						<el-option
							v-for="item in field.options"
							:key="item.value"
							:label="item.label"
							:value="item.value"
						/>
					</el-select>
				</el-form-item>

				<!-- 查询重置按钮 -->
				<el-form-item class="action-item">
					<el-button-group>
						<el-button type="primary" icon="ele-Search" @click="handleQuery">查询</el-button>
						<el-button icon="ele-Refresh" @click="handleReset">重置</el-button>
					</el-button-group>
				</el-form-item>

				<el-form-item>
				</el-form-item>
			</el-form>
		</el-card>

		<!-- 设置弹窗 -->
		<el-dialog v-model="settingVisible" title="查询条件设置" width="450px" draggable :close-on-click-modal="false">
			<!-- 关键字搜索提示 -->
			<div v-if="hasKeywordFields" class="setting-hint">多字段匹配：【{{ keywordFieldLabels }}】</div>

			<!-- 关键字搜索项（不可拖动，显示在提示文字下方） -->
			<div v-if="hasKeywordFields" class="setting-item keyword-item">
				<el-icon class="drag-icon disabled"><ele-Rank /></el-icon>
				<el-checkbox v-model="keywordVisible" class="setting-checkbox">
					关键字
				</el-checkbox>
			</div>

			<div class="setting-hint">拖动调整显示顺序，勾选控制显示隐藏</div>
			<div class="setting-content" ref="settingListRef">
				<!-- 其他字段项（可拖动） -->
				<div
					v-for="(field, index) in tempSettingFields"
					:key="field.prop"
					class="setting-item"
					:class="{ 'is-dragging': dragIndex === index }"
					draggable="true"
					@dragstart="(e) => onDragStart(e, index)"
					@dragover.prevent="(e) => onDragOver(e, index)"
					@drop="(e) => onDrop(e, index)"
					@dragend="onDragEnd"
				>
					<el-icon class="drag-icon"><ele-Rank /></el-icon>
					<el-checkbox v-model="field.visible" class="setting-checkbox">
						{{ field.label || field.prop }}
					</el-checkbox>
				</div>
			</div>
			<template #footer>
				<el-button @click="settingVisible = false">取消</el-button>
				<el-button @click="resetSetting">恢复默认</el-button>
				<el-button type="primary" @click="saveSetting">确定</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { CompareEnum } from './types';
import type { QueryCondition, SearchField, SettingField } from './types';

// 缓存键名
const STORAGE_KEY = 'AdvancedSearch';

const props = defineProps({
	fields: {
		type: Array as () => SearchField[],
		required: true,
	},
	// 模块标识，用于区分不同页面的缓存
	mode: {
		type: String,
		default: '',
	},
	// 关键字搜索字段列表，传入后会在第一项显示关键字输入框
	keywordFields: {
		type: Array as () => string[],
		default: () => [],
	},
	// 禁止挂载时自动触发查询（由父组件控制初始查询时机）
	disableAutoQuery: {
		type: Boolean,
		default: false,
	},
});

const emit = defineEmits(['query', 'reset']);

const route = useRoute();
const formRef = ref();
const queryParams = reactive<Record<string, any>>({});

// 关键字搜索
const keyword = ref('');
const hasKeywordFields = computed(() => props.keywordFields && props.keywordFields.length > 0);

// 关键字匹配的字段标签（从 searchFields 中匹配 prop，显示 label 值）
const keywordFieldLabels = computed(() => {
	if (!hasKeywordFields.value) return '';
	return props.keywordFields.map((fieldName) => {
		const field = props.fields.find((f) => f.prop === fieldName);
		return field?.label || fieldName;
	}).join('、');
});

// 设置相关
const settingVisible = ref(false);
const settingFields = ref<SettingField[]>([]);
const tempSettingFields = ref<SettingField[]>([]);
const visibleProps = ref<string[]>([]);
const settingListRef = ref();
// 关键字可见性
const keywordVisible = ref(true);

// 拖拽相关
const dragIndex = ref(-1);
const dragOverIndex = ref(-1);

// 获取模块标识
const getMode = (): string => {
	if (props.mode) return props.mode;
	// 使用当前路由，去除斜杠和参数
	return route.path.replace(/^\//, '').replace(/\/.*$/, '').replace(/\?.*$/, '') || 'default';
};

// 获取所有缓存数据
const getAllCache = (): Record<string, any> => {
	const cached = localStorage.getItem(STORAGE_KEY);
	if (cached) {
		try {
			return JSON.parse(cached);
		} catch (e) {
			return {};
		}
	}
	return {};
};

// 保存缓存数据
const saveAllCache = (data: Record<string, any>) => {
	localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
};

// 从缓存读取当前模块的设置
const loadSettingFromCache = (): { props: string[]; order: string[]; keywordVisible?: boolean } | null => {
	const allCache = getAllCache();
	const mode = getMode();
	return allCache[mode]?.setting || null;
};

// 保存当前模块的设置到缓存
const saveSettingToCache = (propsList: string[], order: string[], keywordVisible?: boolean) => {
	const allCache = getAllCache();
	const mode = getMode();
	if (!allCache[mode]) allCache[mode] = {};
	allCache[mode].setting = { props: propsList, order, keywordVisible };
	saveAllCache(allCache);
};

// 从缓存读取查询参数
const loadQueryParams = (): Record<string, any> | null => {
	const allCache = getAllCache();
	const mode = getMode();
	return allCache[mode]?.params || null;
};

// 保存查询参数到缓存
const saveQueryParamsToCache = (params: Record<string, any>) => {
	const allCache = getAllCache();
	const mode = getMode();
	if (!allCache[mode]) allCache[mode] = {};
	allCache[mode].params = params;
	saveAllCache(allCache);
};

// 清除查询参数缓存
const clearQueryParams = () => {
	const allCache = getAllCache();
	const mode = getMode();
	if (allCache[mode]) {
		delete allCache[mode].params;
		saveAllCache(allCache);
	}
};

// 初始化设置状态
const initSettingState = () => {
	const cached = loadSettingFromCache();

	if (cached && cached.order && cached.order.length > 0) {
		visibleProps.value = cached.props || cached.order;

		const orderMap = new Map(cached.order.map((p, i) => [p, i]));
		const sortedFields = [...props.fields].sort((a, b) => {
			const orderA = orderMap.has(a.prop) ? orderMap.get(a.prop)! : 999;
			const orderB = orderMap.has(b.prop) ? orderMap.get(b.prop)! : 999;
			return orderA - orderB;
		});

		settingFields.value = sortedFields.map((f) => ({
			label: f.label,
			prop: f.prop,
			visible: visibleProps.value.includes(f.prop),
		}));

		// 恢复关键字可见性
		if (cached.keywordVisible !== undefined) {
			keywordVisible.value = cached.keywordVisible;
		}
	} else {
		visibleProps.value = props.fields.map((f) => f.prop);
		settingFields.value = props.fields.map((f) => ({
			label: f.label,
			prop: f.prop,
			visible: true,
		}));
		keywordVisible.value = true;
	}
};

// 可见字段
const visibleFields = computed(() => {
	return settingFields.value
		.filter((f) => f.visible)
		.map((sf) => props.fields.find((f) => f.prop === sf.prop))
		.filter(Boolean) as SearchField[];
});

// 根据字段类型获取默认比对方式
const getDefaultCompare = (field: SearchField): CompareEnum => {
	if (field.compare) return field.compare;

	switch (field.type) {
		case 'string':
		case undefined:
			return CompareEnum.Like;
		case 'number':
			return CompareEnum.Eq;
		case 'numberRange':
			return CompareEnum.Between;
		case 'date':
			return CompareEnum.Eq;
		case 'dateRange':
			return CompareEnum.Between;
		case 'dic':
		case 'select':
			return CompareEnum.Eq;
		case 'dicRange':
			return CompareEnum.In;
		default:
			return CompareEnum.Eq;
	}
};

// 构建查询条件
const buildQueryConditions = (): QueryCondition[] => {
	const conditions: QueryCondition[] = [];

	// 关键字搜索条件不在这里处理，通过 keyword 属性单独传递

	visibleFields.value.forEach((field) => {
		const value = queryParams[field.prop];

		if (field.type === 'numberRange') {
			const startValue = queryParams[`${field.prop}Start`];
			const endValue = queryParams[`${field.prop}End`];
			if (startValue !== undefined && endValue !== undefined) {
				conditions.push({
					field: field.prop,
					value: [startValue, endValue],
					compare: CompareEnum.Between,
				});
			} else if (startValue !== undefined) {
				conditions.push({
					field: field.prop,
					value: startValue,
					compare: CompareEnum.Ge,
				});
			} else if (endValue !== undefined) {
				conditions.push({
					field: field.prop,
					value: endValue,
					compare: CompareEnum.Le,
				});
			}
		} else if (field.type === 'dateRange') {
			if (value && Array.isArray(value) && value.length === 2) {
				conditions.push({
					field: field.prop,
					value: value,
					compare: CompareEnum.Between,
				});
			}
		} else if (field.type === 'dicRange') {
			if (value && Array.isArray(value) && value.length > 0) {
				conditions.push({
					field: field.prop,
					value: value,
					compare: CompareEnum.In,
				});
			}
		} else {
			if (value !== undefined && value !== null && value !== '') {
				conditions.push({
					field: field.prop,
					value: value,
					compare: getDefaultCompare(field),
				});
			}
		}
	});

	return conditions;
};

// 初始化查询参数
const initParams = () => {
	const cachedParams = loadQueryParams();

	props.fields.forEach((field) => {
		if (field.type === 'numberRange') {
			queryParams[`${field.prop}Start`] = cachedParams?.[`${field.prop}Start`] ?? field.defaultValue?.[0] ?? undefined;
			queryParams[`${field.prop}End`] = cachedParams?.[`${field.prop}End`] ?? field.defaultValue?.[1] ?? undefined;
		} else if (field.type === 'dateRange' || field.type === 'dicRange') {
			queryParams[field.prop] = cachedParams?.[field.prop] ?? field.defaultValue ?? [];
		} else {
			queryParams[field.prop] = cachedParams?.[field.prop] ?? field.defaultValue ?? undefined;
		}
	});

	// 恢复关键字值
	if (hasKeywordFields.value && cachedParams?._keyword) {
		keyword.value = cachedParams._keyword;
	}

	// 如果有缓存参数且未禁用自动查询，自动触发查询
	if (!props.disableAutoQuery && cachedParams && Object.keys(cachedParams).length > 0) {
		setTimeout(() => {
			handleQuery();
		}, 100);
	}
};

onMounted(() => {
	initSettingState();
	initParams();
});

watch(() => props.fields, () => {
	initSettingState();
	initParams();
}, { deep: true });

// 获取字典选项
const getDicOptions = (field: SearchField) => {
	if (field.options) return field.options;
	return [];
};

// 打开设置
const openSetting = () => {
	tempSettingFields.value = settingFields.value.map((f) => ({ ...f }));
	settingVisible.value = true;
};

// 保存设置
const saveSetting = () => {
	const selectedProps = tempSettingFields.value
		.filter((f) => f.visible)
		.map((f) => f.prop);

	if (selectedProps.length === 0) {
		tempSettingFields.value[0].visible = true;
		selectedProps.push(tempSettingFields.value[0].prop);
	}

	settingFields.value = tempSettingFields.value.map((f) => ({ ...f }));
	visibleProps.value = selectedProps;

	const order = tempSettingFields.value.map((f) => f.prop);
	saveSettingToCache(selectedProps, order, keywordVisible.value);

	// 清空之前输入框的缓存内容
	clearQueryParams();

	// 关键字取消勾选时清除keyword的值
	if (!keywordVisible.value) {
		keyword.value = '';
	}

	settingVisible.value = false;
};

// 恢复默认
const resetSetting = () => {
	tempSettingFields.value = props.fields.map((f) => ({
		label: f.label,
		prop: f.prop,
		visible: true,
	}));
	keywordVisible.value = true;
};

// 拖拽
const onDragStart = (e: DragEvent, index: number) => {
	dragIndex.value = index;
	if (e.dataTransfer) e.dataTransfer.effectAllowed = 'move';
};

const onDragOver = (e: DragEvent, index: number) => {
	e.preventDefault();
	dragOverIndex.value = index;
};

const onDrop = (e: DragEvent, index: number) => {
	e.preventDefault();
	if (dragIndex.value === -1 || dragIndex.value === index) return;

	const items = [...tempSettingFields.value];
	const [removed] = items.splice(dragIndex.value, 1);
	items.splice(index, 0, removed);
	tempSettingFields.value = items;

	dragIndex.value = -1;
	dragOverIndex.value = -1;
};

const onDragEnd = () => {
	dragIndex.value = -1;
	dragOverIndex.value = -1;
};

// 查询
const handleQuery = () => {
	console.log('handleQuery 被调用');
	const conditions = buildQueryConditions();
	console.log('查询条件:', conditions);

	// 保存查询参数到缓存（包括关键字）
	const paramsToSave = { ...queryParams };
	if (hasKeywordFields.value) {
		paramsToSave._keyword = keyword.value;
	}
	saveQueryParamsToCache(paramsToSave);

	emit('query', conditions);
};

// 重置
const handleReset = () => {
	props.fields.forEach((field) => {
		if (field.type === 'numberRange') {
			queryParams[`${field.prop}Start`] = undefined;
			queryParams[`${field.prop}End`] = undefined;
		} else if (field.type === 'dateRange' || field.type === 'dicRange') {
			queryParams[field.prop] = [];
		} else {
			queryParams[field.prop] = undefined;
		}
	});

	// 清除关键字
	keyword.value = '';

	clearQueryParams();
	emit('reset', []);
};

const getQueryParams = () => {
	return buildQueryConditions();
};

// 获取关键字值
const getKeyword = () => {
	return keyword.value;
};

const setQueryParams = (params: Record<string, any>) => {
	Object.keys(params).forEach((key) => {
		if (key in queryParams) {
			queryParams[key] = params[key];
		}
	});
};

defineExpose({ getQueryParams, getKeyword, setQueryParams, handleQuery, handleReset });
</script>

<style scoped lang="scss">
.advanced-search-container {
	:deep(.el-form--inline .el-form-item) {
		margin-bottom: 10px !important;
	}

	:deep(.el-form-item__label) {
		padding-right: 8px;
	}

	.setting-item {
		margin-right: 15px !important;
	}

	.action-item {
		margin-left: auto;
	}
}

.setting-hint {
	font-size: 12px;
	color: #909399;
	margin-bottom: 12px;
}

.keyword-item {
	display: flex;
	align-items: center;
	padding: 8px 12px;
	border: 1px solid #e4e7ed;
	border-radius: 4px;
	margin-bottom: 6px;
	cursor: default;
	background: #fff;

	&:hover {
		background: #f5f7fa;
		border-color: #c0c4cc;
	}

	.drag-icon {
		color: #c0c4cc;
		margin-right: 10px;

		&.disabled {
			opacity: 0.5;
		}
	}

	.setting-checkbox {
		flex: 1;
	}
}

.setting-content {
	max-height: 400px;
	overflow-y: auto;

	.setting-item {
		display: flex;
		align-items: center;
		padding: 8px 12px;
		border: 1px solid #e4e7ed;
		border-radius: 4px;
		margin-bottom: 6px;
		cursor: grab;
		transition: all 0.2s;
		background: #fff;

		&:hover {
			background: #f5f7fa;
			border-color: #c0c4cc;
		}

		&.is-dragging {
			opacity: 0.5;
			border-style: dashed;
		}

		.drag-icon {
			color: #c0c4cc;
			margin-right: 10px;
			cursor: grab;

			&:active {
				cursor: grabbing;
			}
		}

		.setting-checkbox {
			flex: 1;
		}
	}
}
</style>
