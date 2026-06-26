#!/usr/bin/env node
const fs = require("fs");
const fsPromises = fs.promises;
const path = require("path");
const os = require("os");
const AdmZip = require("adm-zip");

// 动态适配打包环境的axios引入
let axios;
try {
  axios = require("axios");
} catch (e) {
  const axiosPath = path.join(__dirname, "node_modules/axios/dist/node/axios.cjs");
  if (fs.existsSync(axiosPath)) {
    axios = require(axiosPath);
  } else {
    throw new Error("axios 模块未找到，请检查依赖安装");
  }
}

/**
 * 解析命令行参数，获取配置文件路径
 * 支持格式:
 *   swagger-generator --config 路径
 *   swagger-generator -c 路径
 *   swagger-generator 路径.json
 */
function getConfigPathFromArgs() {
  const args = process.argv.slice(2);

  // 查找 --config 或 -c 参数
  const configArgIndex = args.findIndex(arg => arg === "--config" || arg === "-c");
  if (configArgIndex !== -1 && args.length > configArgIndex + 1) {
    let configPath = args[configArgIndex + 1];
    if (!path.isAbsolute(configPath)) {
      configPath = path.join(process.cwd(), configPath);
    }
    return configPath;
  }

  // 检查第一个参数是否是 JSON 文件路径
  if (args.length > 0 && args[0].endsWith(".json")) {
    let configPath = args[0];
    if (!path.isAbsolute(configPath)) {
      configPath = path.join(process.cwd(), configPath);
    }
    return configPath;
  }

  // 默认使用程序同目录下的 config.json
  let defaultPath;
  if (process.pkg) {
    // 打包后的环境
    defaultPath = path.join(path.dirname(process.execPath), "config.json");
  } else {
    defaultPath = path.join(__dirname, "config.json");
  }
  return defaultPath;
}

/**
 * 处理输出路径
 */
function resolveOutputPath(configuredPath) {
  if (path.isAbsolute(configuredPath)) {
    return configuredPath;
  }
  return path.join(process.cwd(), configuredPath);
}

/**
 * 删除指定的文件或文件夹
 */
async function deletePath(targetPath) {
  try {
    const stats = await fsPromises.stat(targetPath);
    if (stats.isDirectory()) {
      const files = await fsPromises.readdir(targetPath);
      for (const file of files) {
        await deletePath(path.join(targetPath, file));
      }
      await fsPromises.rmdir(targetPath);
      console.log(`已删除文件夹: ${targetPath}`);
    } else {
      await fsPromises.unlink(targetPath);
      console.log(`已删除文件: ${targetPath}`);
    }
  } catch (error) {
    if (error.code !== "ENOENT") {
      console.error(`删除 ${targetPath} 时出错:`, error.message);
    }
  }
}

/**
 * 主函数
 */
async function generateDownloadAndCleanClient() {
  try {
    // 获取配置文件路径
    const configPath = getConfigPathFromArgs();
    console.log(`配置文件路径: ${configPath}`);

    // 验证配置文件是否存在
    if (!fs.existsSync(configPath)) {
      console.error(`错误：配置文件不存在: ${configPath}`);
      console.log("\n使用方法:");
      console.log("  swagger-generator --config <配置文件路径>");
      console.log("  swagger-generator -c <配置文件路径>");
      console.log("  swagger-generator <配置文件路径.json>");
      console.log("\n配置文件格式:");
      console.log(JSON.stringify({
        apiurl: "http://localhost:5005/swagger/Default/swagger.json",
        outPath: "./src/api-services",
        Delete: [".swagger-codegen", ".gitignore"]
      }, null, 2));
      process.exit(1);
    }

    // 读取配置文件
    let config;
    try {
      const configContent = fs.readFileSync(configPath, "utf8");
      config = JSON.parse(configContent);
    } catch (error) {
      console.error(`解析配置文件失败: ${error.message}`);
      process.exit(1);
    }

    // 验证必要字段
    if (!config.apiurl || !config.outPath) {
      console.error("错误：配置文件必须包含 apiurl 和 outPath 字段");
      process.exit(1);
    }

    const { apiurl, outPath, Delete: itemsToDelete = [] } = config;

    // 1. 获取API规范
    console.log(`\n正在从 ${apiurl} 获取API规范...`);
    const getResponse = await axios.get(apiurl);
    const apiSpec = getResponse.data;
    console.log("成功获取API规范，准备生成客户端代码...");

    // 2. 构建请求数据
    const requestData = {
      spec: apiSpec,
      lang: "typescript-axios",
      type: "CLIENT",
    };

    // 3. 发送生成请求
    console.log("正在向Swagger生成器发送请求...");
    const postResponse = await axios.post(
      "https://generator3.swagger.io/api/generate",
      requestData,
      {
        responseType: "arraybuffer",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/octet-stream",
        },
      }
    );

    // 4. 保存临时压缩包
    const tempZipPath = path.join(os.tmpdir(), "temp-client.zip");
    fs.writeFileSync(tempZipPath, postResponse.data);
    console.log(`客户端代码压缩包已保存到临时目录: ${tempZipPath}`);

    // 5. 处理输出路径
    const resolvedOutPath = resolveOutputPath(outPath);
    console.log(`准备将文件输出到: ${resolvedOutPath}`);

    if (!fs.existsSync(resolvedOutPath)) {
      fs.mkdirSync(resolvedOutPath, { recursive: true });
      console.log(`已创建输出目录: ${resolvedOutPath}`);
    }

    // 6. 解压文件
    console.log(`正在解压文件到 ${resolvedOutPath} ...`);
    const zip = new AdmZip(tempZipPath);
    zip.extractAllTo(resolvedOutPath, true);
    console.log("文件解压完成");

    // 7. 清理临时文件
    fs.unlinkSync(tempZipPath);
    console.log("临时压缩包已清理");

    // 8. 删除不需要的文件
    if (itemsToDelete.length > 0) {
      console.log("\n开始清理不需要的文件和文件夹...");
      for (const item of itemsToDelete) {
        const itemPath = path.join(resolvedOutPath, item);
        await deletePath(itemPath);
      }
    }

    console.log(`\n✅ 所有操作完成，文件已保存到：${resolvedOutPath}`);
  } catch (error) {
    console.error("\n❌ 操作失败:", error.message);
    if (error.response) {
      console.error("响应状态码:", error.response.status);
    } else if (error.code === "ENOENT") {
      console.error("路径错误:", error.path);
    }
    process.exit(1);
  }
}

generateDownloadAndCleanClient();
