<template>
	<div class="button-bar" :class="`button-bar--${displayStyle}`">
		<template v-for="(group, groupKey) in visibleConfig" :key="groupKey">
			<!-- 按钮组：包含子按钮 -->
			<el-button-group v-if="isGroup(group)">
				<template v-for="(item, itemKey) in group.childs" :key="itemKey">
					<!-- 普通按钮 -->
					<el-tooltip v-if="item.type === 'button'" :content="item.label" :disabled="barSettings.showLabel" placement="top">
						<el-button
							:type="item.color || 'primary'"
							:size="barSettings.size"
							:icon="displayStyle === 'inline' && barSettings.showIcon ? (item.icon || undefined) : undefined"
							:class="{ 'icon-only': !barSettings.showLabel }"
							@click="onButtonClick(itemKey, item)"
						>
							<span v-if="barSettings.showLabel" class="btn-content" :class="`btn-content--${displayStyle}`">
								<i v-if="barSettings.showIcon && item.icon" :class="item.icon"></i>
								<span>{{ item.label }}</span>
							</span>
						</el-button>
					</el-tooltip>
					<!-- 下拉按钮 -->
					<el-dropdown v-else-if="item.type === 'list'" trigger="click">
						<el-tooltip :content="item.label" :disabled="barSettings.showLabel" placement="top">
							<el-button
								:type="item.color || 'primary'"
								:size="barSettings.size"
								:icon="displayStyle === 'inline' && barSettings.showIcon ? (item.icon || undefined) : undefined"
								:class="{ 'icon-only': !barSettings.showLabel }"
							>
								<span v-if="barSettings.showLabel" class="btn-content" :class="`btn-content--${displayStyle}`">
									<i v-if="barSettings.showIcon && item.icon" :class="item.icon"></i>
									<span>{{ item.label }}</span>
								</span>
								<el-icon class="el-icon--right"><arrow-down /></el-icon>
							</el-button>
						</el-tooltip>
						<template #dropdown>
							<el-dropdown-menu>
								<el-dropdown-item
									v-for="(child, childKey) in item.childs"
									:key="childKey"
									:icon="displayStyle === 'inline' && barSettings.showIcon ? (child.icon || undefined) : undefined"
									@click="onButtonClick(childKey, child)"
								>
									<span v-if="displayStyle === 'vertical' && barSettings.showIcon && child.icon" class="dropdown-item-icon">
										<i :class="child.icon"></i>
									</span>
									{{ child.label }}
								</el-dropdown-item>
							</el-dropdown-menu>
						</template>
					</el-dropdown>
				</template>
			</el-button-group>

			<!-- 独立按钮（无 childs 的顶层按钮） -->
			<template v-if="isButton(group)">
				<el-tooltip v-if="group.type === 'button'" :content="group.label" :disabled="barSettings.showLabel" placement="top">
					<el-button
						:type="group.color || 'primary'"
						:size="barSettings.size"
						:icon="displayStyle === 'inline' && barSettings.showIcon ? (group.icon || undefined) : undefined"
						:class="{ 'icon-only': !barSettings.showLabel }"
						@click="onButtonClick(groupKey, group)"
					>
						<span v-if="barSettings.showLabel" class="btn-content" :class="`btn-content--${displayStyle}`">
							<i v-if="barSettings.showIcon && group.icon" :class="group.icon"></i>
							<span>{{ group.label }}</span>
						</span>
					</el-button>
				</el-tooltip>
				<el-dropdown v-else-if="group.type === 'list'" trigger="click">
					<el-tooltip :content="group.label" :disabled="barSettings.showLabel" placement="top">
						<el-button
							:type="group.color || 'primary'"
							:size="barSettings.size"
							:icon="displayStyle === 'inline' && barSettings.showIcon ? (group.icon || undefined) : undefined"
							:class="{ 'icon-only': !barSettings.showLabel }"
						>
							<span v-if="barSettings.showLabel" class="btn-content" :class="`btn-content--${displayStyle}`">
								<i v-if="barSettings.showIcon && group.icon" :class="group.icon"></i>
								<span>{{ group.label }}</span>
							</span>
							<el-icon class="el-icon--right"><arrow-down /></el-icon>
						</el-button>
					</el-tooltip>
					<template #dropdown>
						<el-dropdown-menu>
							<el-dropdown-item
								v-for="(child, childKey) in group.childs"
								:key="childKey"
								:icon="displayStyle === 'inline' && barSettings.showIcon ? (child.icon || undefined) : undefined"
								@click="onButtonClick(childKey, child)"
							>
								<span v-if="displayStyle === 'vertical' && barSettings.showIcon && child.icon" class="dropdown-item-icon">
									<i :class="child.icon"></i>
								</span>
								{{ child.label }}
							</el-dropdown-item>
						</el-dropdown-menu>
					</template>
				</el-dropdown>
			</template>
		</template>

		<!-- 设置按钮 -->
		<div class="button-bar__settings">
			<el-tooltip content="按钮栏设置" placement="top">
				<el-button text circle @click="settingsVisible = true">
					<el-icon :size="16"><setting /></el-icon>
				</el-button>
			</el-tooltip>
		</div>

		<!-- 设置弹窗 -->
		<el-dialog v-model="settingsVisible" title="按钮栏设置" width="360px" append-to-body>
			<el-form label-width="100px">
				<el-form-item label="按钮大小">
					<el-radio-group v-model="barSettings.size">
						<el-radio-button value="small">小</el-radio-button>
						<el-radio-button value="default">默认</el-radio-button>
						<el-radio-button value="large">大</el-radio-button>
					</el-radio-group>
				</el-form-item>
				<el-form-item label="显示图标">
					<el-switch v-model="barSettings.showIcon" :disabled="!canToggleIcon" @change="onShowIconChange" />
				</el-form-item>
				<el-form-item label="显示文本">
					<el-switch v-model="barSettings.showLabel" :disabled="!canToggleLabel" @change="onShowLabelChange" />
				</el-form-item>
			</el-form>
			<template #footer>
				<el-button @click="settingsVisible = false">取消</el-button>
				<el-button type="primary" @click="saveSettings">保存</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup>
import { computed, onMounted, reactive, ref } from 'vue';
import { ArrowDown, Setting } from '@element-plus/icons-vue';
import { useUserInfo } from '/@/stores/userInfo';
import { Local } from '/@/utils/storage';
import { ElMessage } from 'element-plus';

/**
 * 按钮配置项
 */
interface ButtonItem {
	type: 'button' | 'list';
	label?: string;
	icon?: string;
	color?: string;
	click?: ((...args: any[]) => void) | null;
	childs?: Record<string, ButtonItem>;
}

/**
 * 按钮组配置
 */
interface ButtonGroup {
	type: 'group';
	label?: string;
	childs: Record<string, ButtonItem>;
	[key: string]: any;
}

/**
 * 按钮栏设置
 */
interface BarSettings {
	size: 'small' | 'default' | 'large';
	showIcon: boolean;
	showLabel: boolean;
}

const props = withDefaults(defineProps<{
	/** 当前模块前缀，如 "sysUser" */
	mode: string;
	/** 按钮配置对象 */
	buttonConfig: Record<string, ButtonGroup | ButtonItem>;
	/** 按钮显示样式: inline=图标文字一行, vertical=图标文字换行居中 */
	displayStyle?: 'inline' | 'vertical';
	/** 按钮点击回调，参数为按钮 key */
	onButtonClick?: (key: string, item: ButtonItem) => void;
}>(), {
	displayStyle: 'inline',
});

const stores = useUserInfo();
const authBtnList = computed(() => stores.userInfos.authBtnList ?? []);

// ========== 设置相关 ==========
const settingsVisible = ref(false);
const defaultSettings: BarSettings = { size: 'default', showIcon: true, showLabel: true };
const barSettings = reactive<BarSettings>({ ...defaultSettings });

const loadSettings = () => {
	const cached = Local.get(`buttonBar_${props.mode}`);
	if (cached) {
		barSettings.size = cached.size ?? defaultSettings.size;
		barSettings.showIcon = cached.showIcon ?? defaultSettings.showIcon;
		barSettings.showLabel = cached.showLabel ?? defaultSettings.showLabel;
	}
};

// 图标和文本不能同时隐藏
const canToggleIcon = computed(() => barSettings.showLabel);
const canToggleLabel = computed(() => barSettings.showIcon);

const onShowIconChange = (val: boolean) => {
	if (!val && !barSettings.showLabel) {
		barSettings.showLabel = true;
	}
};

const onShowLabelChange = (val: boolean) => {
	if (!val && !barSettings.showIcon) {
		barSettings.showIcon = true;
	}
};

const saveSettings = () => {
	Local.set(`buttonBar_${props.mode}`, {
		size: barSettings.size,
		showIcon: barSettings.showIcon,
		showLabel: barSettings.showLabel,
	});
	settingsVisible.value = false;
	ElMessage.success('设置已保存');
};

onMounted(() => {
	loadSettings();
});

// ========== 按钮过滤逻辑 ==========
const isGroup = (item: any): item is ButtonGroup => {
	return item && item.type === 'group' && item.childs;
};

const isButton = (item: any): item is ButtonItem => {
	return item && (item.type === 'button' || item.type === 'list') && !item.childs;
};

const hasAuth = (authKey: string): boolean => {
	const fullKey = `${props.mode}:${authKey}`;
	return authBtnList.value.some((v: string) => v === fullKey);
};

const visibleConfig = computed(() => {
	const result: Record<string, any> = {};

	for (const [key, group] of Object.entries(props.buttonConfig)) {
		if (isGroup(group)) {
			const visibleChilds: Record<string, ButtonItem> = {};
			for (const [childKey, child] of Object.entries(group.childs)) {
				if (child.type === 'list' && child.childs) {
					const visibleItems: Record<string, ButtonItem> = {};
					for (const [itemKey, item] of Object.entries(child.childs)) {
						if (hasAuth(itemKey)) {
							visibleItems[itemKey] = item;
						}
					}
					if (Object.keys(visibleItems).length > 0) {
						visibleChilds[childKey] = { ...child, childs: visibleItems };
					}
				} else if (hasAuth(childKey)) {
					visibleChilds[childKey] = child;
				}
			}
			if (Object.keys(visibleChilds).length > 0) {
				result[key] = { ...group, childs: visibleChilds };
			}
		} else if (isButton(group) && hasAuth(key)) {
			result[key] = group;
		}
	}

	return result;
});

const onButtonClick = (key: string, item: ButtonItem) => {
	if (item.click) {
		item.click();
	}
	if (props.onButtonClick) {
		props.onButtonClick(key, item);
	}
};
</script>

<style scoped lang="scss">
.button-bar {
	display: flex;
	flex-wrap: wrap;
	align-items: center;
	gap: 8px;
	padding: 5px;

	:deep(.el-button-group) + :deep(.el-button-group) {
		margin-left: 0;
	}

	&__settings {
		margin-left: auto;
		position: relative;
	}
}

:deep(.el-button.icon-only) {
	padding-right: var(--el-button-padding-vertical);
}

.button-bar--inline {
	.btn-content {
		display: inline-flex;
		align-items: center;
		gap: 2px;
	}
}

.button-bar--vertical {
	.btn-content {
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 2px;
		font-size: 12px;

		i {
			font-size: 14px;
		}
	}

	.dropdown-item-icon {
		display: inline-flex;
		margin-right: 4px;

		i {
			font-size: 14px;
		}
	}
}
</style>
