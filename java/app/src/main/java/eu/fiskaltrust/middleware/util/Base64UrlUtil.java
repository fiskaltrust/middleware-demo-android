package eu.fiskaltrust.middleware.util;

import android.util.Base64;

import java.nio.charset.StandardCharsets;

public final class Base64UrlUtil {

  private static final int FLAGS = Base64.URL_SAFE | Base64.NO_WRAP | Base64.NO_PADDING;

  private Base64UrlUtil() { }

  public static String encode(String text) {
    return Base64.encodeToString(text.getBytes(StandardCharsets.UTF_8), FLAGS);
  }

  public static String decode(String base64Url) {
    return new String(Base64.decode(base64Url, FLAGS), StandardCharsets.UTF_8);
  }
}
